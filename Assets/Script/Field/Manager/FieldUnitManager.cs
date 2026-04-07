namespace Script.Field.Manager
{
    using Script.Asset.Provider;
    using Script.Field.Entity;
    using Script.Field.Data;
    using Script.Field.Component; // IUnitBrainComponent가 있는 네임스페이스
    using Script.Main.Manager.Collection;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> 
    /// FastPool<T>을 활용하여 필드 위 모든 유닛 Entity의 생명주기와 인스턴스를 관리.
    /// Manager 계층 원칙에 따라 Instance-Centric 자료구조(Dictionary)를 사용하여 객체를 식별하고 제어합니다.
    /// </summary>
    public class FieldUnitManager
    {
        // --- Manager State (Instance-Centric) ---
        private readonly Dictionary<long, FieldUnitEntity> _activeUnits;
        private readonly Dictionary<string, FastPool<FieldUnitEntity>> _unitPools;

        private readonly Transform _unitRoot;
        private long _instanceIdCounter = 1;

        public FieldUnitManager(Transform root)
        {
            _activeUnits = new Dictionary<long, FieldUnitEntity>(128);
            _unitPools = new Dictionary<string, FastPool<FieldUnitEntity>>();
            _unitRoot = root;
        }

        // 매개변수에 상세 행동을 결정하는 brainType 추가
        public async Awaitable<FieldUnitEntity> SpawnUnitAsync(string assetAddress, Vector3 position, UnitType type, FieldBrainType brainType)
        {
            FieldUnitEntity entity = null;

            // 1. 풀에서 유효한 객체가 있는지 확인 및 Pop
            if (true == _unitPools.TryGetValue(assetAddress, out FastPool<FieldUnitEntity> pool)
                && true == pool.HasAvailable())
            {
                entity = pool.Pop();
                entity.gameObject.SetActive(true);
                entity.transform.SetPositionAndRotation(position, Quaternion.identity);
            }
            else
            {
                // 2. 풀이 비어있다면 새로 생성 (RepoProvider 계층을 통해 에셋 로드)
                GameObject prefab = await AssetRepoProvider.GetOrNewInstanceAsync(assetAddress);
                if (false == prefab)
                {
                    return null;
                }

                GameObject unitObj = Object.Instantiate(prefab, position, Quaternion.identity, _unitRoot);
                entity = unitObj.GetComponent<FieldUnitEntity>();
                if (false == entity)
                {
                    entity = unitObj.AddComponent<FieldUnitEntity>();
                }

                entity.SetAssetAddress(assetAddress);
            }

            // 3. 고유 ID 부여 및 데이터 문맥(Context) 초기화
            long newId = _instanceIdCounter++;
            UnitRuntimeContext newContext = new UnitRuntimeContext(type, brainType);

            entity.Initialize(newId, newContext);
            _activeUnits.Add(newId, entity);

            // 4. 스크립트 기반 Brain 컴포넌트 조립 (프리팹 분기 대체)
            AttachSpecificBrain(entity, brainType);

            return entity;
        }

        public void DespawnUnit(long instanceId)
        {
            if (true == _activeUnits.TryGetValue(instanceId, out FieldUnitEntity entity))
            {
                string address = entity.AssetAddress;
                entity.Clear();
                entity.gameObject.SetActive(false);

                _activeUnits.Remove(instanceId);

                if (false == _unitPools.TryGetValue(address, out FastPool<FieldUnitEntity> pool))
                {
                    pool = new FastPool<FieldUnitEntity>(32);
                    _unitPools.Add(address, pool);
                }
                pool.Push(entity);
            }
        }

        /// <summary>
        /// 런타임에 유닛의 상세 행동(Brain) 컴포넌트를 스크립트로 조립합니다.
        /// </summary>
        private void AttachSpecificBrain(FieldUnitEntity entity, FieldBrainType brainType)
        {
            // 재사용된 풀링 객체일 수 있으므로, 기존 Brain 정리
            var oldBrains = entity.GetComponents<IUnitBrainComponent>();
            foreach (var oldBrain in oldBrains)
            {
                Object.Destroy((MonoBehaviour)oldBrain);
            }

            IUnitBrainComponent newBrain = null;

            switch (brainType)
            {
                case FieldBrainType.PlayerControl:
                    newBrain = entity.gameObject.AddComponent<PlayerControlBrainComponent>();
                    break;
                case FieldBrainType.PartyFollower:
                    newBrain = entity.gameObject.AddComponent<PartyFollowerBrainComponent>();
                    break;
                case FieldBrainType.NpcShop:
                    newBrain = entity.gameObject.AddComponent<ShopNpcBrainComponent>();
                    break;
                case FieldBrainType.NpcInn:
                    newBrain = entity.gameObject.AddComponent<InnNpcBrainComponent>();
                    break;
                case FieldBrainType.EnemyWanderEncounter:
                    newBrain = entity.gameObject.AddComponent<EnemyWanderEncounterBrainComponent>();
                    break;
                default:
                    // 기본값 혹은 NpcIdle
                    newBrain = entity.gameObject.AddComponent<IdleBrainComponent>();
                    break;
            }

            newBrain?.Initialize(entity);
        }

        public FieldUnitEntity GetUnit(long instanceId)
        {
            _activeUnits.TryGetValue(instanceId, out FieldUnitEntity entity);
            return entity;
        }

        public void UpdateAllUnitsLogic()
        {
            foreach (var entity in _activeUnits.Values)
            {
                entity.ManualUpdate(); // 부착된 Brain에 따라 알아서 다르게 행동함
            }
        }

        public void ClearAll()
        {
            foreach (var entity in _activeUnits.Values)
            {
                if (true == entity && true == entity.gameObject)
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