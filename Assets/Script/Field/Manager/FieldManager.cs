using System;

namespace Script.Global.Manager
{
    using UnityEngine;
    using Unity.Mathematics;
    using System.Collections.Generic;
    using Script.Global.Entity.Data;
    using Script.Field.Entity.Component;
    using Script.Global.Input.Provider;
    using Script.Asset.Provider;
    using Script.Map.Provider;                  
    using Script.Map.Utility;                   
    using Script.Map.Data;                      
    using Script.Map.Entity;                    
    using static Script.Global.Input.Data.Definition; 

    /// <summary>
    /// [Framework] Manager: 필드의 생성, 이동, 맵 동적 로딩 흐름을 총괄 지휘합니다.
    /// </summary>
    public class FieldManager : MonoBehaviour
    {
        [Header("Entity Prefabs")]
        [SerializeField] private MapGridEntity _mapGridPrefab;       // 맵 비주얼 컨테이너 프리팹

        [Header("Map Settings")]
        [SerializeField] private Camera _mainCamera;                 // 가시 영역 계산용 카메라
        [SerializeField] private float _cameraLoadMargin = 64f;      // $XZ$축 로딩 여유분
        [SerializeField] private float _verticalLoadThreshold = 10f; // $Y$축 추가 로딩 경계값
        [SerializeField] private float _layerFadeSpeed = 3f;         // 레이어 전환 투명도 속도

        // --- 외부 데이터 공급자 ---
        private IngameInputProvider _inputProvider; // 입력 신호
        private MapRepoProvider _mapProvider;       // 에셋 공급

        // --- 실시간 관리 인스턴스 ---
        private Entity _playerEntity;
        private PlayerMoveComponent _playerMove;
        private readonly List<PartyMoveComponent> _partyMoves               = new List<PartyMoveComponent>();
        private readonly Dictionary<int, MapGridEntity> _activeGridEntities = new Dictionary<int, MapGridEntity>();
        private readonly HashSet<int> _loadingGrids                         = new HashSet<int>(); // 비동기 로딩 중인 그리드 추적

        // --- 최적화용 상태 변수 ---
        private int _lastGMinX, _lastGMaxX, _lastGMinZ, _lastGMaxZ, _lastYStart, _lastYEnd;
        private int _currentVisibleLayer, _targetVisibleLayer;
        private float _currentFadeAlpha = 1f;
        private bool _isLayerTransitioning;
        private static readonly int _globalMapAlphaID = Shader.PropertyToID("_GlobalMapAlpha");

        private void Awake()
        {
            enabled = false;
        }

        /// <summary>
        /// 상위 시스템에서 진입 시 호출하여 필드를 기동합니다.
        /// </summary>
        public async Awaitable Initialize(IngameInputProvider input, MapRepoProvider map)
        {
            _inputProvider = input;
            _mapProvider = map;
            
            Awaitable taskParty = SpawnPlayerAndPartyAsync(float3.zero, 2); 
            Awaitable taskMap   = UpdateMapGridsAsync(float3.zero, true);

            await taskParty;
            await taskMap;
            
            enabled = true;
        }

        /// <summary>
        /// 플레이어와 파티원(JRPG 방식)을 생성하고 추적 관계를 형성합니다.
        /// </summary>
        private async Awaitable SpawnPlayerAndPartyAsync(float3 spawnPos, int partyCount)
        {
            // 호출이 잦을 것 같지 않으므로 프리팹 참조를 들고 있지 말고 생성하고 차라리 캐싱을 하자.
            GameObject playerObj = await AssetRepoProvider.GetOrNewInstanceAsync("unit_prefab");
            playerObj.transform.SetPositionAndRotation(spawnPos, quaternion.identity);

            _playerEntity        = playerObj.AddComponent<FieldUnitEntity>();
            _playerMove          = playerObj.AddComponent<PlayerMoveComponent>();
            _playerMove.Initialize(_mapProvider);
            
            // instantiate party unit objects
            Transform currentTarget = _playerEntity.transform; 
            for (int i = 0; i < partyCount; ++i)
            {
                GameObject partyObj = await AssetRepoProvider.GetOrNewInstanceAsync("unit_prefab");
                partyObj.transform.SetPositionAndRotation(spawnPos, quaternion.identity);
                
                Entity partyEntity = partyObj.AddComponent<FieldUnitEntity>();
                PartyMoveComponent partyMove = partyObj.AddComponent<PartyMoveComponent>();
                partyMove.Initialize(currentTarget, 0f);
                _partyMoves.Add(partyMove);

                currentTarget = partyEntity.transform; // 줄줄이 이어갈 수 있도록 다음 타겟 변경
            }
        }

        private void Update()
        {
            if (false == _playerMove)
            {
                return;
            }
            
            float deltaTime = Time.deltaTime;

            // 1. 입력 및 이동 (플레이어 -> 파티원 순서 엄수)
            ProcessMovement(deltaTime);
            
            // 2. 카메라 시야 기반 맵 업데이트
            UpdateMapGridsAsync(_playerEntity.transform.position);
            
            // 3. 레이어 전환 효과 처리
            ProcessLayerTransition(deltaTime);
        }

        /// <summary>
        /// 카메라의 4개 모서리 좌표를 지면에 투영하여 현재 로드가 필요한 그리드 영역을 산출합니다.
        /// </summary>
        private async Awaitable UpdateMapGridsAsync(float3 playerPos, bool forceUpdate = false)
        {
            if (false == _mainCamera)
            {
                return;                
            }
            
            // 카메라 시야 영역 계산 (Raycast 활용)
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, playerPos.y, 0));
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;

            Vector2[] viewports = { Vector2.zero, Vector2.up, Vector2.right, Vector2.one };
            foreach (var vp in viewports)
            {
                Ray ray = _mainCamera.ViewportPointToRay(vp);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hit = ray.GetPoint(enter);
                    minX = Mathf.Min(minX, hit.x); maxX = Mathf.Max(maxX, hit.x);
                    minZ = Mathf.Min(minZ, hit.z); maxZ = Mathf.Max(maxZ, hit.z);
                }
            }

            // 여유분(Margin) 적용 및 그리드 인덱스화
            int gMinX = Mathf.FloorToInt((minX - _cameraLoadMargin) / MapConsts.GRID_SIZE);
            int gMaxX = Mathf.FloorToInt((maxX + _cameraLoadMargin) / MapConsts.GRID_SIZE);
            int gMinZ = Mathf.FloorToInt((minZ - _cameraLoadMargin) / MapConsts.GRID_SIZE);
            int gMaxZ = Mathf.FloorToInt((maxZ + _cameraLoadMargin) / MapConsts.GRID_SIZE);

            // $Y$축 조건부 로딩 구간 계산
            float localY = playerPos.y - (math.floor(playerPos.y / MapConsts.GRID_SIZE) * MapConsts.GRID_SIZE);
            int yStart = (localY <= _verticalLoadThreshold) ? -1 : 0;
            int yEnd = (localY >= MapConsts.GRID_SIZE - _verticalLoadThreshold) ? 1 : 0;

            // 범위 변화가 없다면 연산 중단 (최적화)
            if (!forceUpdate && gMinX == _lastGMinX && gMaxX == _lastGMaxX && gMinZ == _lastGMinZ && gMaxZ == _lastGMaxZ && yStart == _lastYStart && yEnd == _lastYEnd) return;

            _lastGMinX = gMinX; _lastGMaxX = gMaxX; _lastGMinZ = gMinZ; _lastGMaxZ = gMaxZ; _lastYStart = yStart; _lastYEnd = yEnd;

            // 타겟 그리드 키 집합 생성
            HashSet<int> targetKeys = new HashSet<int>();
            for (int x = gMinX; x <= gMaxX; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    for (int z = gMinZ; z <= gMaxZ; z++)
                    {
                        float3 pos = new float3(x * MapConsts.GRID_SIZE, y * MapConsts.GRID_SIZE, z * MapConsts.GRID_SIZE);
                        targetKeys.Add(MapCoordUtil.ComputeGridKey(pos));
                    }
                }
            }

            // [언로드] 시야에서 벗어난 그리드 파괴 및 에셋 해제
            List<int> toRemove = new List<int>();
            foreach (var kvp in _activeGridEntities)
            {
                if (!targetKeys.Contains(kvp.Key))
                {
                    kvp.Value.Dispose();
                    Destroy(kvp.Value.gameObject);
                    _mapProvider.ReleaseGridMeshes(kvp.Key); // 메모리 해제 필수
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (int k in toRemove) 
                _activeGridEntities.Remove(k);

            // [로드] 새롭게 시야에 들어온 그리드 비동기 요청
            foreach (int k in targetKeys)
            {
                if (!_activeGridEntities.ContainsKey(k) && !_loadingGrids.Contains(k))
                    await LoadGridVisualAsync(k);
            }
        }

        /// <summary>
        /// 비동기 로드 시 발생하는 동시성 버그(화면 밖으로 나간 그리드의 중복 생성)를 방어하며 로드합니다.
        /// </summary>
        private async Awaitable LoadGridVisualAsync(int gridKey)
        {
            if (!_loadingGrids.Add(gridKey)) 
                return;

            try
            {
                // Provider에 요청하여 메쉬 에셋들을 병렬 로드
                var layers = await _mapProvider.LoadGridMeshesAsync(gridKey);

                // [Cancellation Check] 로드 대기 중 시야에서 사라졌다면 생성을 취소하고 메모리 즉시 반환
                if (!_loadingGrids.Contains(gridKey))
                {
                    _mapProvider.ReleaseGridMeshes(gridKey);
                    return;
                }

                if (layers == null) return;

                // 비주얼 인스턴스화 및 초기화
                MapGridEntity entity = Instantiate(_mapGridPrefab, transform);
                entity.Initialize(layers);
                entity.UpdateLayerVisibility(1 << _currentVisibleLayer);
                
                _activeGridEntities.Add(gridKey, entity);
            }
            finally { _loadingGrids.Remove(gridKey); }
        }

        private void ProcessLayerTransition(float deltaTime)
        {
            if (!_isLayerTransitioning) return;

            if (_currentVisibleLayer != _targetVisibleLayer)
            {
                _currentFadeAlpha -= deltaTime * _layerFadeSpeed;
                if (_currentFadeAlpha <= 0f)
                {
                    _currentFadeAlpha = 0f;
                    _currentVisibleLayer = _targetVisibleLayer;
                    foreach (var g in _activeGridEntities.Values) g.UpdateLayerVisibility(1 << _currentVisibleLayer);
                }
            }
            else
            {
                _currentFadeAlpha += deltaTime * _layerFadeSpeed;
                if (_currentFadeAlpha >= 1f) { _currentFadeAlpha = 1f; _isLayerTransitioning = false; }
            }
            Shader.SetGlobalFloat(_globalMapAlphaID, _currentFadeAlpha);
        }

        public void RequestLayerChange(int layerIdx) { if (_currentVisibleLayer == layerIdx) return; _targetVisibleLayer = layerIdx; _isLayerTransitioning = true; }

        private void ProcessMovement(float deltaTime)
        {
            InputState state = _inputProvider.Current;
            float2 dir = float2.zero;
            if (state.IsPressing(IDxInput.LEFT)) dir.x -= 1; if (state.IsPressing(IDxInput.RIGHT)) dir.x += 1;
            if (state.IsPressing(IDxInput.UP)) dir.y += 1; if (state.IsPressing(IDxInput.DOWN)) dir.y -= 1;

            if (math.lengthsq(dir) > 0) dir = math.normalize(dir);
            _playerMove.ProcessMovement(dir, deltaTime);
            foreach (var m in _partyMoves) m.ProcessMovement(10.5f, deltaTime);
        }
    }
}