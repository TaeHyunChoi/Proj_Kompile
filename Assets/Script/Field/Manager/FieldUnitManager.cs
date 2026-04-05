namespace Script.Field.Manager
{
    using Script.Asset.Provider;
    using Script.Field.Entity;
    using Script.Main.Manager.Collection;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> FastPool<T>을 활용하여 필드 위 모든 유닛 Entity의 생명주기와 인스턴스를 관리 </summary>
    public class FieldUnitManager
    {
        // --- Manager State (Instance-Centric) ---
        private readonly Dictionary<long,   FieldPlayerEntity> _activeUnits;
        private readonly Dictionary<string, FastPool<FieldPlayerEntity>> _unitPools;
        private readonly Transform _unitRoot;

        // 인스턴스 발급용 고유 ID 카운터
        private long _instanceIdCounter = 1;

        public FieldUnitManager(Transform root)
        {
            _activeUnits = new Dictionary<long, FieldPlayerEntity>(32);
            _unitPools = new Dictionary<string, FastPool<FieldPlayerEntity>>();
            _unitRoot = root;
        }

        public async Awaitable<FieldPlayerEntity> SpawnUnitAsync(string assetAddress, Vector3 position)
        {
            FieldPlayerEntity entity;

            // 1. 풀에서 유효한 객체가 있는지 확인 및 Pop
            if (true == _unitPools.TryGetValue(assetAddress, out FastPool<FieldPlayerEntity> pool)
                && true == pool.HasAvailable())
            {
                entity = pool.Pop();
                entity.gameObject.SetActive(true);
                entity.transform.SetPositionAndRotation(position, Quaternion.identity);
            }
            else
            {
                // 2. 풀이 비어있다면 새로 생성 (AssetRepoProvider를 통해 에셋 로드)
                GameObject prefab = await AssetRepoProvider.GetOrNewInstanceAsync("FieldUnitPrefab");
                if (false == prefab)
                {
                    return null;
                }

                GameObject unitObj = Object.Instantiate(prefab, position, Quaternion.identity, _unitRoot);
                if (false == unitObj.TryGetComponent<FieldPlayerEntity>(out entity))
                {
                    entity = unitObj.AddComponent<FieldPlayerEntity>();
                }

                // 풀링 반환 시 사용할 원본 에셋 주소를 엔티티에 기록해둡니다.
                entity.SetAssetAddress(assetAddress);
            }

            // 3. 고유 ID 부여 및 초기화 (재사용 시에도 새로운 ID 발급으로 논리적 실체 완벽 구분)
            long newId = _instanceIdCounter++;
            entity.Initialize(newId);

            _activeUnits.Add(newId, entity);
            return entity;
        }

        public void DespawnUnit(long instanceId)
        {
            if (true == _activeUnits.TryGetValue(instanceId, out FieldPlayerEntity entity))
            {
                // 1. 엔티티 상태 초기화 및 비활성화
                string address = entity.AssetAddress;
                entity.Clear();
                entity.gameObject.SetActive(false);

                // 2. 관리 목록에서 제거
                _activeUnits.Remove(instanceId);

                // 3. FastPool<T>에 Push
                if (false == _unitPools.TryGetValue(address, out FastPool<FieldPlayerEntity> pool))
                {
                    pool = new FastPool<FieldPlayerEntity>(32);
                    _unitPools.Add(address, pool);
                }
                pool.Push(entity);
            }
        }

        // 특정 타입이나 조건을 찾을 때 LINQ를 사용하지 않고 명시적 루프 혹은 키 접근 사용 (GC 최적화)
        public FieldPlayerEntity GetUnit(long instanceId)
        {
            _activeUnits.TryGetValue(instanceId, out FieldPlayerEntity entity);
            return entity;
        }

        public void UpdateAllUnitsLogic()
        {
            // Manager가 Entity들의 흐름을 조율해야 할 경우 수동 업데이트 (예: 턴제 로직 트리거 등)
            foreach (var kvp in _activeUnits)
            {
                kvp.Value.ManualUpdate();
            }
        }

        /// <summary> 씬 전환 등 환경이 완전히 리셋될 때 모든 메모리를 깔끔하게 해제 </summary>
        public void ClearAll()
        {
            // 1. 활성화된 유닛 파괴
            foreach (var kvp in _activeUnits)
            {
                if (false == kvp.Value)
                {
                    Object.Destroy(kvp.Value.gameObject);
                }
            }
            _activeUnits.Clear();

            // 2. 풀 대기 중인 유닛 파괴 및 내부 배열 해제
            foreach (var pool in _unitPools.Values)
            {
                pool.ClearAndDestroyAll();
            }
            _unitPools.Clear();
        }
    }
}