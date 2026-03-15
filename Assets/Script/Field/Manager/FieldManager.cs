namespace Script.Global.Manager
{
    using UnityEngine;
    using Unity.Mathematics;
    using System.Collections.Generic;
    
    // [Framework]에서 분리된 각 계층의 데이터와 컴포넌트를 가져옵니다.
    using Script.Global.Entity.Data;            // Entity 기본 클래스
    using Script.Field.Entity.Component;        // PlayerMoveComponent, PartyMoveComponent
    using Script.Global.Input.Provider;         // 입력 상태 제공자
    using Script.Map.Provider;                  // 맵 에셋 제공자
    using Script.Map.Utility;                   // 좌표, 비트 연산 최적화 유틸리티
    using Script.Map.Data;                      // MapConsts 등 맵 데이터 구조
    using Script.Map.Entity;                    // MapGridEntity 등 맵 비주얼 객체
    using static Script.Global.Input.Data.Definition; // IDxInput 플래그

    /// <summary>
    /// [Framework] Manager: 필드 내의 Entity(Player, Party, MapGrid) 인스턴스들을 관리하고 흐름을 조율합니다.
    /// 1. Provider로부터 입력을 받아 플레이어와 파티원의 이동(JRPG 뱀파이어 궤적)을 제어합니다.
    /// 2. 플레이어의 위치를 기반으로 주변 타일(9-Grid ~ 최대 18-Grid)을 동적 로딩/언로딩합니다.
    /// 3. 레이어(층간) 이동 시 글로벌 쉐이더를 이용한 부드러운 페이드 인/아웃 전환을 처리합니다.
    /// </summary>
    public class FieldManager : MonoBehaviour
    {
        [Header("Entity References (Prefab)")]
        [Tooltip("플레이어, 파티원, 맵 그리드 등 Manager가 생성/관리할 프리팹들입니다.")]
        public Entity PlayerPrefab;
        public Entity PartyMemberPrefab;
        public MapGridEntity MapGridPrefab; 

        [Header("Map Control Settings")]
        [Tooltip("레이어(층) 전환 시 페이드 인/아웃 속도입니다.")]
        public float LayerFadeSpeed = 3f;
        
        [Tooltip("Y축 상/하단 그리드를 로드할 경계 거리입니다. (예: 10이면 0~10 구간에서 아래 그리드, 54~64 구간에서 위 그리드를 추가 로드)")]
        public float VerticalLoadThreshold = 10f; 

        // --- 외부 Provider 캐싱 (Value-Centric 데이터 공급자) ---
        private IngameInputProvider inputProvider;
        private MapRepoProvider mapProvider;

        // --- 관리 대상 Entity 인스턴스 (Instance-Centric) ---
        private Entity playerEntity;
        private PlayerMoveComponent playerMove;
        
        // 파티원들을 순차적으로 업데이트하기 위한 리스트
        private List<PartyMoveComponent> partyMoves = new List<PartyMoveComponent>();

        // --- Map Grid 공간 해싱(Spatial Hashing) 캐싱 관리 ---
        // 플레이어가 현재 위치한 중심 그리드 키
        private int currentPlayerGridKey = -1;
        // 화면과 메모리에 활성화되어 있는 맵 그리드 엔티티들 (Key: GridKey)
        private Dictionary<int, MapGridEntity> activeGridEntities = new Dictionary<int, MapGridEntity>();

        // --- Layer Transition (페이드 전환) 상태 관리 ---
        private int currentVisibleLayer = 0;
        private int targetVisibleLayer = 0;
        private float currentFadeAlpha = 1f;
        private bool isLayerTransitioning = false;

        // 글로벌 쉐이더의 투명도 변수 ID를 캐싱하여 성능(가비지 생성 및 문자열 검색)을 최적화합니다.
        private static readonly int GlobalMapAlphaID = Shader.PropertyToID("_GlobalMapAlpha");

        /// <summary>
        /// 상위 시스템(GameManager 등)에서 진입할 때 Provider들을 주입해주며 초기화합니다.
        /// </summary>
        public void Initialize(IngameInputProvider input, MapRepoProvider map)
        {
            inputProvider = input;
            mapProvider = map;
            
            // 1. 플레이어와 파티원을 스폰합니다. (임시로 float3.zero 위치에 파티원 2명 생성)
            SpawnPlayerAndParty(float3.zero, 2); 
            
            // 2. 초기 위치를 기준으로 주변 맵 그리드 로딩을 강제로 1회 실행합니다.
            UpdateMapGrids(float3.zero, forceUpdate: true);
        }

        /// <summary>
        /// 플레이어와 파티원 엔티티를 생성하고, 서로 꼬리를 물도록 추적 대상을 연결합니다.
        /// </summary>
        private void SpawnPlayerAndParty(float3 spawnPos, int partyCount)
        {
            // [Player 생성]
            playerEntity = Instantiate(PlayerPrefab, spawnPos, Quaternion.identity);
            playerMove = playerEntity.GetComponent<PlayerMoveComponent>();
            
            // MapProvider를 주입하여 Player가 맵 데이터를 기준으로 충돌 및 높이를 계산하게 합니다.
            playerMove.Initialize(mapProvider);

            // [Party 생성 및 꼬리물기 연결]
            Transform currentTarget = playerEntity.transform; // 1번 파티원의 타겟은 플레이어
            float followDistance = 1.0f;

            for (int i = 0; i < partyCount; i++)
            {
                Entity partyEntity = Instantiate(PartyMemberPrefab, spawnPos, Quaternion.identity);
                PartyMoveComponent partyMove = partyEntity.GetComponent<PartyMoveComponent>();
                
                // 앞선 타겟을 환형 버퍼(Ring Buffer) 방식으로 추적하도록 설정
                partyMove.Initialize(currentTarget, followDistance);
                partyMoves.Add(partyMove);

                // 다음 생성될 파티원의 타겟은 방금 생성된 현재 파티원이 됩니다.
                currentTarget = partyEntity.transform;
            }
        }

        /// <summary>
        /// Unity의 Update 루프에서 매 프레임 Entity들의 상태를 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (playerMove == null || inputProvider == null) return;

            float deltaTime = Time.deltaTime;

            // 1. 이동 처리 (플레이어 입력 반영 및 파티원 추적)
            ProcessMovement(deltaTime);

            // 2. 맵 그리드 동적 로드 검사 (플레이어 위치 기반)
            UpdateMapGrids(playerEntity.transform.position);

            // 3. 레이어 전환(Fade In/Out) 애니메이션 처리
            ProcessLayerTransition(deltaTime);
        }

        /// <summary>
        /// Provider의 입력을 읽어와 이동을 처리합니다.
        /// (업데이트 순서가 매우 중요: Player가 먼저 움직여야 Party가 그 궤적을 쫓을 수 있습니다)
        /// </summary>
        private void ProcessMovement(float deltaTime)
        {
            // 1. 누적된(latched) 현재 프레임의 입력 상태 가져오기
            InputState input = inputProvider.Current;
            float2 inputDir = float2.zero;

            // 플래그 검사로 상/하/좌/우 대각선 벡터 생성
            if (input.IsPressing(IDxInput.LEFT))  inputDir.x -= 1f;
            if (input.IsPressing(IDxInput.RIGHT)) inputDir.x += 1f;
            if (input.IsPressing(IDxInput.UP))    inputDir.y += 1f;
            if (input.IsPressing(IDxInput.DOWN))  inputDir.y -= 1f;

            // 벡터 정규화
            if (math.lengthsq(inputDir) > 0f) 
            {
                inputDir = math.normalize(inputDir);
            }

            // 2. 플레이어 선행 이동
            playerMove.ProcessMovement(inputDir, deltaTime);
            
            // 3. 파티원 후행 이동
            // 파티원은 궤적을 놓치지 않기 위해 플레이어보다 속도가 약간 빨라야 합니다. (예: 10.5f)
            float partySpeed = 10.5f; 
            for (int i = 0; i < partyMoves.Count; i++)
            {
                partyMoves[i].ProcessMovement(partySpeed, deltaTime);
            }
        }

        /// <summary>
        /// 플레이어의 위치를 기반으로 기본 9-Grid를 유지하되, 
        /// 높이(Y) 경계에 접근하면 조건부로 위/아래 그리드를 로드해 최대 18-Grid를 캐싱합니다.
        /// </summary>
        private void UpdateMapGrids(float3 playerPos, bool forceUpdate = false)
        {
            // 최적화 유틸을 사용하여 부동소수점 오차 없이 현재 그리드 키 도출
            int newGridKey = MapCoordUtil.ComputeGridKey(playerPos);

            // 이동한 그리드가 이전 프레임과 동일하다면 무거운 연산을 스킵
            if (!forceUpdate && newGridKey == currentPlayerGridKey) return;
            
            currentPlayerGridKey = newGridKey;

            // 1. 현재 그리드 내에서의 로컬 Y 좌표 계산 (0.0f ~ 63.99f)
            // math.floor를 사용해 음수 월드 좌표에서도 안정적인 로컬 값을 구합니다.
            float gridFloorY = math.floor(playerPos.y / MapConsts.GRID_SIZE) * MapConsts.GRID_SIZE;
            float localY = playerPos.y - gridFloorY;

            // 2. Y축 탐색 범위 결정 (기본값은 0, 즉 현재 높이의 평면 9칸만 탐색)
            int yStart = 0;
            int yEnd = 0;

            // 플레이어가 하단 경계(예: 0~10)에 가까우면 바로 아래 그리드(-1) 포함 탐색
            if (localY <= VerticalLoadThreshold)
            {
                yStart = -1;
            }
            // 플레이어가 상단 경계(예: 54~64)에 가까우면 바로 위 그리드(1) 포함 탐색
            if (localY >= (MapConsts.GRID_SIZE - VerticalLoadThreshold))
            {
                yEnd = 1;
            }

            // 3. 이번 프레임에 활성화되어야 할 타겟 그리드 목록 생성
            HashSet<int> targetGrids = new HashSet<int>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = yStart; y <= yEnd; y++) 
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        // X, Y, Z축 오프셋을 적용해 주변 그리드의 위치를 추적
                        float3 offsetPos = playerPos + new float3(
                            x * MapConsts.GRID_SIZE, 
                            y * MapConsts.GRID_SIZE, 
                            z * MapConsts.GRID_SIZE
                        );
                        
                        int adjGridKey = MapCoordUtil.ComputeGridKey(offsetPos);
                        targetGrids.Add(adjGridKey);
                    }
                }
            }

            // 4. 언로드 (Unload): 타겟에 없는 기존 그리드 제거
            // 순회 중 컬렉션 수정을 피하기 위해 삭제할 키를 리스트에 먼저 모읍니다.
            List<int> keysToRemove = new List<int>();
            foreach (var kvp in activeGridEntities)
            {
                if (!targetGrids.Contains(kvp.Key))
                {
                    kvp.Value.Dispose(); // 메모리 정리
                    Destroy(kvp.Value.gameObject); // 비주얼 객체 파괴
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (int key in keysToRemove) 
            {
                activeGridEntities.Remove(key);
            }

            // 5. 로드 (Load): 타겟에 있는데 현재 활성화되지 않은 새 그리드 생성
            foreach (int targetKey in targetGrids)
            {
                if (!activeGridEntities.ContainsKey(targetKey))
                {
                    LoadGridVisualAsync(targetKey);
                }
            }
        }

        /// <summary>
        /// 비주얼 엔티티(MapGridEntity)를 로드하고 계층을 구성합니다.
        /// </summary>
        private async void LoadGridVisualAsync(int gridKey)
        {
            // TODO: 실제 프로젝트에서는 mapProvider를 통해 Addressables 비동기 로드 로직이 들어갑니다.
            // var layerGroups = await mapProvider.LoadGridMeshesAsync(gridKey);
            
            // 프리팹을 생성하고 딕셔너리에 등록 (Manager가 Instance를 제어)
            MapGridEntity newGrid = Instantiate(MapGridPrefab, transform);
            
            // newGrid.Initialize(layerGroups); // 로드된 메쉬 데이터를 전달하여 초기화
            newGrid.UpdateLayerVisibility(1 << currentVisibleLayer); // 현재 시야의 레이어 마스크 적용
            
            activeGridEntities.Add(gridKey, newGrid);
        }

        // ==========================================================
        // Layer Transition (Fade In/Out) Logic
        // ==========================================================

        /// <summary> 
        /// 외부(계단 타일 밟음, 사다리 탑승 등)에서 레이어 변경을 요청할 때 호출합니다. 
        /// </summary>
        public void RequestLayerChange(int newLayerIndex)
        {
            if (currentVisibleLayer == newLayerIndex) return;
            
            targetVisibleLayer = newLayerIndex;
            isLayerTransitioning = true;
        }

        /// <summary>
        /// 맵 전체의 투명도를 글로벌하게 제어하여 드로우콜(Draw Call) 증가 없이 부드럽게 층간을 전환합니다.
        /// </summary>
        private void ProcessLayerTransition(float deltaTime)
        {
            if (!isLayerTransitioning) return;

            // [Phase 1: Fade Out] 화면이 서서히 어두워짐
            if (currentVisibleLayer != targetVisibleLayer)
            {
                currentFadeAlpha -= deltaTime * LayerFadeSpeed;
                
                // 완전히 투명해진 시점(0f)에 물리적 레이어 객체(SetActive)를 스위칭합니다.
                if (currentFadeAlpha <= 0f)
                {
                    currentFadeAlpha = 0f;
                    currentVisibleLayer = targetVisibleLayer;
                    
                    int layerMask = 1 << currentVisibleLayer;
                    foreach (var grid in activeGridEntities.Values)
                    {
                        grid.UpdateLayerVisibility(layerMask);
                    }
                }
            }
            // [Phase 2: Fade In] 화면이 다시 서서히 밝아짐
            else 
            {
                currentFadeAlpha += deltaTime * LayerFadeSpeed;
                
                // 완전히 밝아지면 전환 상태 종료
                if (currentFadeAlpha >= 1f)
                {
                    currentFadeAlpha = 1f;
                    isLayerTransitioning = false;
                }
            }

            // 쉐이더의 글로벌 알파값을 조절합니다. 
            // 맵 타일의 머티리얼 쉐이더 내부에 이 '_GlobalMapAlpha' 값을 참조하여 투명도를 조절하는 로직이 있어야 합니다.
            Shader.SetGlobalFloat(GlobalMapAlphaID, currentFadeAlpha);
        }
    }
}