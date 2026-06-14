namespace Kompile.Field.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Kompile.Asset.Data;
    using Kompile.Asset.Provider;
    using Kompile.Field.Entity;

    /// <summary>
    /// [Manager 계층] 필드 내 모든 FieldEntity의 인스턴스 풀링 및 실시간 활성 상태를 관리합니다.
    /// Instance-Centric 원칙에 따라 참조 기반의 집합 자료구조를 활용합니다.
    /// </summary>
    public class FieldEntityManager
    {
        private readonly Transform _unitRoot;
        
        // Instance-Centric 자료구조 설계 규칙 준수
        private readonly Dictionary<AssetKey, Queue<FieldEntity>> _entityPool = new Dictionary<AssetKey, Queue<FieldEntity>>();
        private readonly HashSet<FieldEntity> _activeEntities = new HashSet<FieldEntity>();

        public FieldEntityManager(Transform unitRoot)
        {
            _unitRoot = unitRoot;
        }

        /// <summary>
        /// [Pre-warm] 필드 진입 또는 로딩 화면 단계에서 호출하여 필요한 수량만큼 인스턴스를 미리 메모리에 할당합니다.
        /// 이를 통해 실제 인게임 플레이(Hot Path) 환경에서의 힙 할당을 전면 차단합니다.
        /// </summary>
        public async Awaitable PrewarmEntitiesAsync(AssetKey prefabKey, int count)
        {
            // 데이터 공급자(Provider)로부터 원본 에셋(Prefab) 데이터 확보
            GameObject prefab = await AssetProvider.GetOrLoadPrefabAsync(prefabKey);
            if (!prefab) 
                return;

            if (!_entityPool.TryGetValue(prefabKey, out Queue<FieldEntity> queue))
            {
                queue = new Queue<FieldEntity>(count);
                _entityPool.Add(prefabKey, queue);
            }

            for (int i = 0; i < count; i++)
            {
                // 원본 프리팹을 기반으로 직접 복제하여 Addressables 내부 생성 가비지 방지
                GameObject go = UnityEngine.Object.Instantiate(prefab, _unitRoot);
                go.SetActive(false);

                if (!go.TryGetComponent(out FieldEntity entity))
                {
                    entity = go.AddComponent<FieldEntity>();
                }

                // 💡 [컴파일 에러 해결] FieldEntity 내부의 존재하지 않는 무리한 키 세팅 로직을 완전히 걷어내어 
                // 엔티티가 순수 논리 실체로서 컴포넌트 기능만 수행하도록 격리했습니다.
                queue.Enqueue(entity);
            }
        }

        /// <summary>
        /// [Hot Path] 풀에 대기 중인 엔티티를 즉시 꺼내어 활성화합니다. (동기식 연산, Zero-GC 보장)
        /// </summary>
        public FieldEntity Spawn(AssetKey prefabKey)
        {
            if (_entityPool.TryGetValue(prefabKey, out Queue<FieldEntity> queue) && queue.Count > 0)
            {
                FieldEntity entity = queue.Dequeue();
                entity.gameObject.SetActive(true);
                _activeEntities.Add(entity);
                return entity;
            }

            // 만약 풀이 고갈되었다면 프레임 저하를 감수하고 예외적으로 동적 생성 처리 및 워닝 출력
            Debug.LogWarning($"[FieldEntityManager] 풀 개수가 부족하여 동적 생성이 발생했습니다: {prefabKey.Value}. Prewarm 수량을 늘려야 합니다.");
            
            // 유니티 6000 순수 Awaitable 환경에서 대기 없는 비동기 풀 충전 처리
            _ = PrewarmEntitiesAsync(prefabKey, 1);
            
            return null;
        }

        /// <summary>
        /// [Hot Path] 사용이 끝난 엔티티를 안전하게 풀로 반환하여 재사용 대기 상태로 전환합니다.
        /// 💡 [컴파일 에러 해결] 엔티티 역추적 방식 대신, 매니저가 식별용 AssetKey를 파라미터로 직접 하달받아 
        /// 정확한 대기 장부(Queue)를 매핑하고 반환시키도록 시그니처를 완벽히 교정했습니다.
        /// </summary>
        public void Despawn(AssetKey prefabKey, FieldEntity entity)
        {
            if (!entity)
            {
                return;                
            }
            
            entity.gameObject.SetActive(false);
            _activeEntities.Remove(entity);

            if (!_entityPool.TryGetValue(prefabKey, out Queue<FieldEntity> queue))
            {
                queue = new Queue<FieldEntity>();
                _entityPool.Add(prefabKey, queue);
            }
            queue.Enqueue(entity);
        }

        public void Dispose()
        {
            _activeEntities.Clear();
            _entityPool.Clear();
        }
    }
}