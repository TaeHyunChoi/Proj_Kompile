namespace Script.Field.Manager
{
    using Script.Asset.Provider; // (혹은 설정하신 Script.Asset.RepoProvider 경로 사용)
    using Script.Field.Entity;
    using Script.Field.Data;
    using Script.Main.Manager.Collection;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> 
    /// FastPool<T>을 활용하여 필드 위 모든 유닛 Entity의 생명주기와 인스턴스를 관리.
    /// Manager 계층 원칙에 따라 Instance-Centric 자료구조(Dictionary, HashSet)를 사용하여 객체를 식별하고 제어합니다.
    /// </summary>
    public class FieldUnitManager
    {
        // --- Manager State (Instance-Centric) ---
        private readonly Dictionary<long, FieldUnitEntity> _activeUnits;
        private readonly Dictionary<string, FastPool<FieldUnitEntity>> _unitPools;

        // 타입별 빠른 접근을 위한 캐싱 컬렉션 (LINQ 검색을 대체하여 GC 할당 방지)
        private readonly HashSet<FieldUnitEntity> _playerUnits;
        private readonly HashSet<FieldUnitEntity> _enemyUnits;
        private readonly HashSet<FieldUnitEntity> _npcUnits;

        private readonly Transform _unitRoot;

        // 인스턴스 발급용 고유 ID 카운터
        private long _instanceIdCounter = 1;

        public FieldUnitManager(Transform root)
        {
            _activeUnits = new Dictionary<long, FieldUnitEntity>(128);
            _unitPools = new Dictionary<string, FastPool<FieldUnitEntity>>();

            // 성능 및 메모리 효율을 위해 초기 용량(Capacity) 할당
            _playerUnits = new HashSet<FieldUnitEntity>(4);
            _enemyUnits = new HashSet<FieldUnitEntity>(64);
            _npcUnits = new HashSet<FieldUnitEntity>(32);

            _unitRoot = root;
        }

        public async Awaitable<FieldUnitEntity> SpawnUnitAsync(string assetAddress, Vector3 position, UnitType type)
        {
            FieldUnitEntity entity;

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
                    // 프리팹에 컴포넌트가 누락된 경우를 대비한 방어 코드
                    entity = unitObj.AddComponent<FieldUnitEntity>();
                }

                // 풀링 반환 시 사용할 원본 에셋 주소를 엔티티에 기록해둡니다.
                entity.SetAssetAddress(assetAddress);
            }

            // 3. 고유 ID 부여 및 데이터 문맥(Context) 초기화 (재사용 시에도 새로운 ID 발급으로 완벽 구분)
            long newId = _instanceIdCounter++;
            UnitRuntimeContext newContext = new UnitRuntimeContext(type);

            entity.Initialize(newId, newContext);
            _activeUnits.Add(newId, entity);

            // 4. 타입별 고속 접근을 위한 그룹화
            CategorizeUnit(entity, true);

            return entity;
        }

        public void DespawnUnit(long instanceId)
        {
            if (true == _activeUnits.TryGetValue(instanceId, out FieldUnitEntity entity))
            {
                // 1. 그룹 관리 목록에서 선제적 제거
                CategorizeUnit(entity, false);

                // 2. 엔티티 상태 초기화 및 비활성화
                string address = entity.AssetAddress;
                entity.Clear();
                entity.gameObject.SetActive(false);

                // 3. 전체 활성 목록에서 제거
                _activeUnits.Remove(instanceId);

                // 4. FastPool<T>에 Push
                if (false == _unitPools.TryGetValue(address, out FastPool<FieldUnitEntity> pool))
                {
                    pool = new FastPool<FieldUnitEntity>(32);
                    _unitPools.Add(address, pool);
                }
                pool.Push(entity);
            }
        }

        private void CategorizeUnit(FieldUnitEntity entity, bool isAdd)
        {
            // LINQ와 foreach 검색을 피하기 위해 관리 시점에 HashSet으로 분류합니다.
            switch (entity.Context.Type)
            {
                case UnitType.Player:
                case UnitType.PartyGroup:
                    if (isAdd) _playerUnits.Add(entity);
                    else _playerUnits.Remove(entity);
                    break;

                case UnitType.Enemy:
                    if (isAdd) _enemyUnits.Add(entity);
                    else _enemyUnits.Remove(entity);
                    break;

                case UnitType.NPC:
                    if (isAdd) _npcUnits.Add(entity);
                    else _npcUnits.Remove(entity);
                    break;
            }
        }

        // 특정 조건이나 단일 개체를 찾을 때 명시적 키 접근 사용 (GC 최적화)
        public FieldUnitEntity GetUnit(long instanceId)
        {
            _activeUnits.TryGetValue(instanceId, out FieldUnitEntity entity);
            return entity;
        }

        // GC 할당 없이 순회할 수 있도록 HashSet 자체를 읽기 전용으로 참조할 수 있게 제공합니다.
        public HashSet<FieldUnitEntity> GetPlayerUnits() => _playerUnits;
        public HashSet<FieldUnitEntity> GetEnemyUnits() => _enemyUnits;
        public HashSet<FieldUnitEntity> GetNpcUnits() => _npcUnits;

        public void UpdateAllUnitsLogic()
        {
            // Manager가 Entity들의 흐름을 조율해야 할 경우 수동 업데이트 (예: 턴제 로직 트리거 등)
            // .Values 콜렉션에 대한 foreach는 내부적으로 struct Enumerator를 사용하여 GC를 발생시키지 않습니다.
            foreach (var entity in _activeUnits.Values)
            {
                entity.ManualUpdate();
            }
        }

        /// <summary> 씬 전환 등 환경이 완전히 리셋될 때 모든 메모리와 인스턴스를 해제 </summary>
        public void ClearAll()
        {
            // 1. 활성화된 유닛 파괴
            foreach (var entity in _activeUnits.Values)
            {
                if (true == entity && true == entity.gameObject)
                {
                    Object.Destroy(entity.gameObject);
                }
            }

            // 모든 관리 목록 비우기
            _activeUnits.Clear();
            _playerUnits.Clear();
            _enemyUnits.Clear();
            _npcUnits.Clear();

            // 2. 풀에 대기 중인 유닛 파괴 및 풀 자료구조 초기화
            foreach (var pool in _unitPools.Values)
            {
                pool.ClearAndDestroyAll();
            }
            _unitPools.Clear();
        }
    }
}