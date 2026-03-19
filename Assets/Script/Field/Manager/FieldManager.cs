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
        [SerializeField] private MapGridEntity _mapGridPrefab;

        [Header("Map Settings")]
        [SerializeField] private Camera 
            _mainCamera;

        [SerializeField] private float 
            _cameraLoadMargin      = 16f,
            _verticalLoadThreshold = 10f,
            _layerFadeSpeed        = 3f;

        private IngameInputProvider
            _inputProvider;

        private MapRepoProvider
            _mapProvider;

        private Entity 
            _playerEntity;

        private PlayerMoveComponent 
            _playerMove;

        private readonly List<PartyMoveComponent> 
            _partyMoves = new List<PartyMoveComponent>();

        private readonly Dictionary<int, MapGridEntity> 
            _activeGridEntities = new Dictionary<int, MapGridEntity>();

        private readonly HashSet<int> 
            _loadingGrids = new HashSet<int>();

        private static readonly int
            _globalMapAlphaID = Shader.PropertyToID("_GlobalMapAlpha");

        private int 
            _lastGMinX, 
            _lastGMaxX, 
            _lastGMinZ, 
            _lastGMaxZ, 
            _lastYStart, 
            _lastYEnd,
            _currentVisibleLayer,
            _targetVisibleLayer;

        private float 
            _currentFadeAlpha = 1f;

        private bool 
            _isLayerTransitioning;

        private void Awake()
        {
            enabled = false;
        }

        public async Awaitable Initialize(IngameInputProvider input, MapRepoProvider map)
        {
            _inputProvider = input;
            _mapProvider   = map;

            // 파티 생성 완료 대기
            await SpawnPlayerAndPartyAsync(float3.zero, 2);

            // 초기 맵 로드 트리거 (UpdateMapGrids를 동기로 호출하여 내부적으로 비동기 로딩을 시작시킵니다)
            UpdateMapGrids(_playerEntity.transform.position, forceUpdate: true);

            enabled = true;
        }

        private async Awaitable SpawnPlayerAndPartyAsync(float3 spawnPos, int partyCount)
        {
            GameObject playerObj = await AssetRepoProvider.GetOrNewInstanceAsync("unit_prefab");
            playerObj.transform.SetPositionAndRotation(spawnPos, quaternion.identity);

            _playerEntity = playerObj.AddComponent<FieldUnitEntity>();
            _playerMove = playerObj.AddComponent<PlayerMoveComponent>();
            _playerMove.Initialize(_mapProvider);

            Transform currentTarget = _playerEntity.transform;
            for (int i = 0; i < partyCount; ++i)
            {
                GameObject partyObj = await AssetRepoProvider.GetOrNewInstanceAsync("unit_prefab");
                partyObj.transform.SetPositionAndRotation(spawnPos, quaternion.identity);

                Entity partyEntity = partyObj.AddComponent<FieldUnitEntity>();
                PartyMoveComponent partyMove = partyObj.AddComponent<PartyMoveComponent>();
                partyMove.Initialize(currentTarget, 0f);
                _partyMoves.Add(partyMove);

                currentTarget = partyEntity.transform;
            }
        }

        // [수정] Update 루프가 없으면 맵이 실시간으로 로드되지 않으므로 복구합니다.
        private void Update()
        {
            if (false == _playerMove) return;

            float deltaTime = Time.deltaTime;

            ProcessMovement(deltaTime);

            // 실시간 카메라 시야 기반 맵 업데이트 (동기 호출)
            UpdateMapGrids(_playerEntity.transform.position);

            ProcessLayerTransition(deltaTime);
        }

        private void UpdateMapGrids(float3 playerPos, bool forceUpdate = false)
        {
            if (false == _mainCamera)
            {
                return;
            }

            Vector3 camPos = _mainCamera.transform.position;
            float minX = Mathf.Min(playerPos.x, camPos.x);
            float maxX = Mathf.Max(playerPos.x, camPos.x);
            float minZ = Mathf.Min(playerPos.z, camPos.z);
            float maxZ = Mathf.Max(playerPos.z, camPos.z);

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, playerPos.y, 0));
            Vector2[] viewports = { Vector2.zero, Vector2.up, Vector2.right, Vector2.one };

            foreach (var vp in viewports)
            {
                Ray ray = _mainCamera.ViewportPointToRay(vp);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    enter = Mathf.Min(150f, enter);
                    Vector3 hit = ray.GetPoint(enter);

                    minX = Mathf.Min(minX, hit.x); maxX = Mathf.Max(maxX, hit.x);
                    minZ = Mathf.Min(minZ, hit.z); maxZ = Mathf.Max(maxZ, hit.z);
                }
            }

            minX -= _cameraLoadMargin;
            minZ -= _cameraLoadMargin;

            maxX += _cameraLoadMargin;
            maxZ += _cameraLoadMargin;

            float inverse_grid_size = 1 / MapConsts.GRID_SIZE;
            int gMinX = Mathf.FloorToInt(minX * inverse_grid_size);
            int gMaxX = Mathf.FloorToInt(maxX * inverse_grid_size);
            int gMinZ = Mathf.FloorToInt(minZ * inverse_grid_size);
            int gMaxZ = Mathf.FloorToInt(maxZ * inverse_grid_size);

            int playerGridY = Mathf.FloorToInt(playerPos.y * inverse_grid_size);
            float localY = playerPos.y - (playerGridY * MapConsts.GRID_SIZE);

            int yOffsetStart = (localY <= _verticalLoadThreshold) ? -1 : 0;
            int yOffsetEnd   = (localY >= MapConsts.GRID_SIZE - _verticalLoadThreshold) ? 1 : 0;

            if (false == forceUpdate 
                && gMinX == _lastGMinX 
                && gMaxX == _lastGMaxX 
                && gMinZ == _lastGMinZ 
                && gMaxZ == _lastGMaxZ 
                && yOffsetStart == _lastYStart 
                && yOffsetEnd == _lastYEnd)
                return;

            _lastGMinX = gMinX;
            _lastGMaxX = gMaxX;
            _lastGMinZ = gMinZ;
            _lastGMaxZ = gMaxZ;
            _lastYStart = yOffsetStart;
            _lastYEnd = yOffsetEnd;

            HashSet<int> targetGrids = new HashSet<int>();
            for (int x = gMinX; x <= gMaxX; x++)
            {
                for (int yOffset = yOffsetStart; yOffset <= yOffsetEnd; ++yOffset)
                {
                    int targetGridY = playerGridY + yOffset;
                    for (int z = gMinZ; z <= gMaxZ; z++)
                    {
                        float3 offsetPos = MapConsts.GRID_SIZE * new float3(x, targetGridY, z);
                        targetGrids.Add(MapCoordUtil.ComputeGridKey(offsetPos));
                    }
                }
            }

            List<int> keysToRemove = new List<int>();
            foreach (var kvp in _activeGridEntities)
            {
                if (false == targetGrids.Contains(kvp.Key))
                {
                    kvp.Value.Dispose();

                    if (kvp.Value.gameObject != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }

                    _mapProvider.ReleaseGridMeshes(kvp.Key);
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (int key in keysToRemove) _activeGridEntities.Remove(key);

            List<int> loadingToCancel = new List<int>();
            foreach (int loadingKey in _loadingGrids)
            {
                if (!targetGrids.Contains(loadingKey))
                {
                    loadingToCancel.Add(loadingKey);
                }
            }
            foreach (int cancelKey in loadingToCancel)
            {
                _loadingGrids.Remove(cancelKey);
            }


            // [핵심 수정] await를 제거하여 모든 그리드 생성이 동시에(Parallel) 트리거
            foreach (int targetKey in targetGrids)
            {
                if (true == _activeGridEntities.ContainsKey(targetKey)
                    || true == _loadingGrids.Contains(targetKey))
                {
                    continue;
                }

                LoadGridVisualAsync(targetKey);
            }
        }

        private async void LoadGridVisualAsync(int gridKey)
        {
            if (!_loadingGrids.Add(gridKey))
            {
                return;
            }

            try
            {
                // 1. TextAsset 데이터를 로드하고 존재하는지 검사합니다.
                bool hasData = await _mapProvider.LoadGridDataAsync(gridKey);

                // 대기 시간 동안 카메라 시야를 벗어났다면 취소합니다.
                if (false == _loadingGrids.Contains(gridKey))
                {
                    return;
                }

                // 데이터가 없는 그리드(맵의 끝, 절벽 밖 등)라면 여기서 로딩을 조용히 끝냅니다.
                if (false == hasData)
                {
                    return;
                }

                // 2. 데이터가 존재함이 확인되었으므로 안심하고 메쉬들을 병렬 로드합니다.
                var layers = await _mapProvider.LoadGridMeshesAsync(gridKey);

                if (!_loadingGrids.Contains(gridKey))
                {
                    if (layers != null && layers.Count > 0)
                    {
                        _mapProvider.ReleaseGridMeshes(gridKey);
                    }

                    return;
                }

                if (layers == null || layers.Count == 0)
                {
                    return;
                }

                // 3. 비주얼 인스턴스화 및 초기화
                MapGridEntity entity = Instantiate(_mapGridPrefab, transform);
                entity.Initialize(layers);
                entity.UpdateLayerVisibility(1 << _currentVisibleLayer);

                _activeGridEntities.Add(gridKey, entity);
            }
            finally
            {
                _loadingGrids.Remove(gridKey);
            }
        }

        private void ProcessLayerTransition(float deltaTime)
        {
            if (false == _isLayerTransitioning)
            {
                return;
            }

            if (_currentVisibleLayer != _targetVisibleLayer)
            {
                _currentFadeAlpha -= deltaTime * _layerFadeSpeed;
                if (_currentFadeAlpha <= 0f)
                {
                    _currentFadeAlpha = 0f;
                    _currentVisibleLayer = _targetVisibleLayer;
                    foreach (var g in _activeGridEntities.Values)
                    {
                        g.UpdateLayerVisibility(1 << _currentVisibleLayer);
                    }
                }
            }
            else
            {
                _currentFadeAlpha += deltaTime * _layerFadeSpeed;
                if (_currentFadeAlpha >= 1f)
                {
                    _currentFadeAlpha = 1f; _isLayerTransitioning = false; 
                }
            }
            Shader.SetGlobalFloat(_globalMapAlphaID, _currentFadeAlpha);
        }

        public void RequestLayerChange(int layerIdx) 
        {
            if (_currentVisibleLayer != layerIdx)
                _targetVisibleLayer = layerIdx; _isLayerTransitioning = true;
        }

        private void ProcessMovement(float deltaTime)
        {
            InputState state = _inputProvider.Current;
            float2 dir = float2.zero;

            if (state.IsPressing(IDxInput.LEFT)) dir.x -= 1; 
            if (state.IsPressing(IDxInput.RIGHT)) dir.x += 1;
            if (state.IsPressing(IDxInput.UP)) dir.y += 1; 
            if (state.IsPressing(IDxInput.DOWN)) dir.y -= 1;

            if (math.lengthsq(dir) > 0) dir = math.normalize(dir);
            _playerMove.ProcessMovement(dir, deltaTime);
            foreach (var m in _partyMoves) m.ProcessMovement(10.5f, deltaTime);
        }
    }
}