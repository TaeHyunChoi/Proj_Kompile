using Script.Global.Unit.Entity;

namespace Script.Field.Manager
{
    using Script.Global.Asset.Data;
    using Script.Global.Asset.Provider;
    using Script.Global.Unit.Data;
    using Script.Main.Manager.Collection; // FastPool<T> (가정)
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> 
    /// Instance-Centric 자료구조(Dictionary)를 사용하여 유닛 객체를 식별하고 제어하는 조립 공장.
    /// </summary>
    public class FieldUnitManager
    {
        private readonly Dictionary<long, UnitEntityBase> _activeUnits;
        private readonly Dictionary<string, FastPool<UnitEntityBase>> _unitPools;
        private readonly Transform _unitRoot;
        private long _instanceIdCounter = 1;

        public FieldUnitManager(Transform root)
        {
            _activeUnits = new Dictionary<long, UnitEntityBase>(128);
            _unitPools = new Dictionary<string, FastPool<UnitEntityBase>>();
            _unitRoot = root;
        }

        /// <summary>
        /// [진입점] 기획 ID를 통해 원본 데이터를 로드하고 유닛을 조립 및 스폰합니다.
        /// </summary>
        public async Awaitable<UnitEntityBase> SpawnUnitByIdAsync(int unitId, Vector3 position)
        {
            UnitTableData tableData = UnitTableProvider.GetUnitData(unitId);
            if (tableData.UnitID == 0)
            {
                return null;
            }

            // 내부 스폰 로직 호출
            UnitEntityBase spawnedEntity = await SpawnUnitInternalAsync(
                tableData.AssetAddress, 
                position, 
                tableData.Type, 
                tableData.BrainType
            );

            return spawnedEntity;
        }

        private async Awaitable<UnitEntityBase> SpawnUnitInternalAsync(string assetAddress, Vector3 position, UnitType type, UnitBrainType brainType)
        {
            UnitEntityBase entity = null;

            // 1. 풀 확인
            if (_unitPools.TryGetValue(assetAddress, out FastPool<UnitEntityBase> pool) && pool.HasAvailable())
            {
                entity = pool.Pop();
                entity.gameObject.SetActive(true);
                entity.transform.SetPositionAndRotation(position, Quaternion.identity);
            }
            else
            {
                // 2. 에셋 생성
                GameObject prefab = await AssetProvider.GetOrNewInstanceAsync(assetAddress);
                if (prefab == false) return null;

                GameObject unitObj = Object.Instantiate(prefab, position, Quaternion.identity, _unitRoot);
                entity = unitObj.GetComponent<UnitEntityBase>();
                if (entity == false) entity = unitObj.AddComponent<UnitEntityBase>();

                entity.SetAssetAddress(assetAddress);
            }

            // 3. ID 발급 및 Runtime Context(상태 데이터) 초기화
            long newId = _instanceIdCounter++;
            UnitRuntimeContext newContext = new UnitRuntimeContext(type, brainType);
            
            entity.Initialize(newId, newContext);
            _activeUnits.Add(newId, entity);

            // 4. 순수 C# Brain 컴포넌트 조립 (AddComponent 오버헤드 제로)
            AttachSpecificBrain(entity, brainType);

            return entity;
        }

        /// <summary> 런타임에 유닛의 상세 행동(Brain)을 C# 객체로 할당합니다. </summary>
        private void AttachSpecificBrain(UnitEntityBase entity, UnitBrainType brainType)
        {
            IUnitBrain newBrain = null;

            switch (brainType)
            {
                case UnitBrainType.PlayerControl:
                    newBrain = new PlayerControlBrain();
                    break;
                default:
                    break;
            }

            entity.SetBrain(newBrain);
        }

        public void DespawnUnit(long instanceId)
        {
            if (_activeUnits.TryGetValue(instanceId, out UnitEntityBase entity))
            {
                string address = entity.AssetAddress;
                
                // Clear 호출 시 내부의 C# Brain도 함께 Clear됨
                entity.Clear();
                entity.gameObject.SetActive(false);

                _activeUnits.Remove(instanceId);

                if (_unitPools.TryGetValue(address, out FastPool<UnitEntityBase> pool) == false)
                {
                    pool = new FastPool<UnitEntityBase>(32);
                    _unitPools.Add(address, pool);
                }
                pool.Push(entity);
            }
        }

        public UnitEntityBase GetUnit(long instanceId)
        {
            _activeUnits.TryGetValue(instanceId, out UnitEntityBase entity);
            return entity;
        }

        public void UpdateAllUnitsLogic()
        {
            foreach (var entity in _activeUnits.Values)
            {
                entity.ManualUpdate();
            }
        }

        public void ClearAll()
        {
            foreach (var entity in _activeUnits.Values)
            {
                if (entity && entity.gameObject)
                {
                    Object.Destroy(entity.gameObject);
                }
            }

            _activeUnits.Clear();

            foreach (var pool in _unitPools.Values)
            {
                pool.ClearAndDestroyAll();
            }
            _unitPools.Clear();
        }
    }
}