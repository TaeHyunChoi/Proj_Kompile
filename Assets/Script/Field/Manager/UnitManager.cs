namespace Script.Global.Unit.Manager
{
    using Script.Field.Data;
    using Script.Field.Entity;
    using Script.Asset.Data;
    using Script.Asset.Provider;
    using Script.Unit.Data;
    using Script.Unit.Entity;
    using Script.Main.Manager.Collection;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// [Framework] Instance-Centric 자료구조(Dictionary)를 사용하여 유닛 객체를 식별하고 제어하는 조립 공장.
    /// </summary>
    public class UnitManager
    {
        private readonly Dictionary<long, UnitEntityBase> _activeUnits;
        private readonly Dictionary<string, FastPool<UnitEntityBase>> _unitPools;
        private readonly Transform _unitRoot;
        private readonly IMapQueryService _mapQueryService;
        private long _instanceIdCounter = 1;

        public UnitManager(Transform root, IMapQueryService mapQueryService)
        {
            _activeUnits = new Dictionary<long, UnitEntityBase>(128);
            _unitPools = new Dictionary<string, FastPool<UnitEntityBase>>();
            _unitRoot = root;
            _mapQueryService = mapQueryService;
        }

        /// <summary>
        /// [진입점] 기획 ID를 통해 원본 데이터를 로드하고 유닛을 조립 및 스폰합니다.
        /// </summary>
        public async Awaitable<UnitEntityBase> SpawnUnitByIDAsync(int unitId, Vector3 position)
        {
            UnitTableData tableData = UnitTableProvider.GetUnitData(unitId);
            if (tableData.ID == 0)
            {
                return null;
            }

            UnitEntityBase spawnedEntity = await SpawnUnitInternalAsync(
                tableData.Address,
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
            if (_unitPools.TryGetValue(assetAddress, out FastPool<UnitEntityBase> pool) 
                && pool.HasAvailable())
            {
                entity = pool.Pop();
                entity.gameObject.SetActive(true);
                entity.transform.SetPositionAndRotation(position, Quaternion.identity);
            }
            else
            {
                // 2. 에셋 생성
                GameObject prefab = await AssetProvider.GetOrNewInstanceAsync(assetAddress);
                if (prefab == false)
                {
                    return null;
                }

                GameObject unitObj = Object.Instantiate(prefab, position, Quaternion.identity, _unitRoot);
                
                // [수정됨] UnitEntityBase는 abstract이므로 AddComponent 불가. 
                // Prefab에 이미 구체 클래스(FieldPlayerEntity 등)가 붙어있어야 함.
                entity = unitObj.GetComponent<UnitEntityBase>();
                if (entity == null)
                {
                    Debug.LogError($"[FieldUnitManager] 에셋에 UnitEntityBase를 상속받은 컴포넌트가 없습니다! Address: {assetAddress}");
                    Object.Destroy(unitObj);
                    return null;
                }
                
                // AssetKey 세팅 (string -> AssetKey 암시적 변환이나 래핑을 지원한다고 가정)
                entity.SetAssetKey(new AssetKey(assetAddress)); 
            }

            // 3. ID 발급 및 Runtime Context(상태 데이터) 초기화
            long newId = _instanceIdCounter++;
            UnitRuntimeContext newContext = new UnitRuntimeContext(type, brainType);

            entity.Initialize(newId, newContext);
            _activeUnits.Add(newId, entity);

            // 4. FieldPlayerEntity라면 IMapQueryService 주입
            if (entity is FieldPlayerEntity fieldPlayer)
            {
                fieldPlayer.SetMapQuery(_mapQueryService);
            }

            // 5. 순수 C# Brain 컴포넌트 조립
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
                // [수정됨] AssetAddress 대신 Entity의 Key를 사용하여 주소 문자열을 얻음
                // (AssetKey 구현 형태에 따라 .Value, .ID 등으로 접근 방식은 변경하실 수 있습니다)
                string address = entity.Key.ToString(); 

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