namespace Kompile.Domain
{
    using Data;
    using UnityEngine;
    using Unity.Mathematics;
    using System.Collections.Generic;
    
    /// <summary> 맵 오브젝트 인스턴스 스폰, 시각적 트랜지션, 큐 기반 동기적 스트리밍 제어 (Instance-Centric) </summary>
    public class MapMgr : GameLogicMgrBase
    {
        private MapProvider _mapProvider;
        
        // --- Manage: Map ---
        private readonly Dictionary<int, List<MapChunk>> _spawnedMapObjects = new Dictionary<int, List<MapChunk>>();
        private Transform _rootTransform;


        // --- Streaming State ---
        private readonly HashSet<int> _activeGrids = new HashSet<int>();
        private readonly HashSet<int> _loadingGrids = new HashSet<int>();
        private readonly List<int> _gridsToRemove = new List<int>();
        private readonly HashSet<int> _keepGrids = new HashSet<int>();
        private HashSet<int> _validGridKeys = new HashSet<int>();

        // --- Optimization Caches ---
        private readonly Dictionary<string, string> _materialAddressCache = new Dictionary<string, string>();
        private readonly List<MapChunk> _animatingChunksCache = new List<MapChunk>();

        // --- Rendering & Visuals ---
        private readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();
        private static readonly int ColorPropID = Shader.PropertyToID("_Color");
        private int _layerTransitionToken = 0;
        private ushort _lastLayerMask = ushort.MaxValue;

        // --- Camera & Streaming Config ---
        private Transform CameraTransform => InCamera.Main.transform;

        private bool _isStreamingActive = false;
        private float _streamTimer = CHECK_INTERVAL;

        private const float PRELOAD_RADIUS = 10f;
        private const float UNLOAD_RADIUS = 20f;
        private const float CHECK_INTERVAL = 1f;
        private const float GRID_SIZE = 64f;
        private const float GRID_SIZE_RECIP = 1f / 64f;

        public MapProvider Provider => _mapProvider;

#pragma warning disable 1998
        public override void RegisterToCache()
        {
            InGame.Register(this);
        }
        public override async Awaitable<bool> OnAwake()
        {
            Prior = 2.1f;
            _mapProvider = new MapProvider();

            Transform mapRoot = new GameObject("Map").transform;
            mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mapRoot.SetParent(InGame.Transform);
            _rootTransform = mapRoot.transform;

            return true;
        }
#pragma warning restore 1998
        public override async Awaitable<bool> OnUpdate()
        {
            if (!_isStreamingActive)
            {
                return false;
            }

            bool update = await ProcessRequests();
            
            _streamTimer += Time.deltaTime;
            if (_streamTimer >= CHECK_INTERVAL)
            {
                _streamTimer = 0f;
                CheckAndTriggerStreaming();
            }

            return update;
        }

        protected override async Awaitable<bool> HandleRequestAsync(RequestBase request)
        {
            await Awaitable.NextFrameAsync();
            
            switch (request.Type)
            {
                default:
                    break;
            }
            
            request.ReturnToPool();
            return true;
        }

        // --- 시스템 제어 ---
        public void PlayStreaming(HashSet<int> validGridKeys)
        {
            _isStreamingActive = true;
            _streamTimer = CHECK_INTERVAL;

            _validGridKeys.Clear();
            foreach (var gridKey in validGridKeys)
            {
                _validGridKeys.Add(gridKey);
            }
        }
        public void StopStreaming()
        {
            _isStreamingActive = false;
        }
        public void DisposeAll()
        {
            StopStreaming();

            List<MapChunk> chunks;
            foreach (var kvp in _spawnedMapObjects)
            {
                chunks = kvp.Value;
                for (int i = 0; i < chunks.Count; ++i)
                {
                    if (chunks[i].Obj) Object.Destroy(chunks[i].Obj);
                }
            }

            _spawnedMapObjects.Clear();
            _activeGrids.Clear();
            _loadingGrids.Clear();
            _gridsToRemove.Clear();
            _keepGrids.Clear();
            _validGridKeys.Clear();
            _animatingChunksCache.Clear();

            _mapProvider?.Dispose();
        }

        // --- 스트리밍 (OnUpdate()에서 동기 호출) ---
        private void CheckAndTriggerStreaming()
        {
            if (!CameraTransform)
            {
                return;
            }

            float Y_RADIUS = GRID_SIZE;
            float unloadRadSq = UNLOAD_RADIUS * UNLOAD_RADIUS;
            float preloadRadSq = PRELOAD_RADIUS * PRELOAD_RADIUS;

            int keepRange = Mathf.CeilToInt(UNLOAD_RADIUS * GRID_SIZE_RECIP) + 1;
            int yRange = Mathf.CeilToInt(Y_RADIUS * GRID_SIZE_RECIP);

            Vector3 camPos = CameraTransform.position;
            float camX = camPos.x;
            float camY = camPos.y;
            float camZ = camPos.z;

            int camGx = Mathf.FloorToInt(camX * GRID_SIZE_RECIP);
            int camGy = Mathf.FloorToInt(camY * GRID_SIZE_RECIP);
            int camGz = Mathf.FloorToInt(camZ * GRID_SIZE_RECIP);

            _keepGrids.Clear();

            for (int dy = -yRange; dy <= yRange; ++dy)
            {
                for (int dx = -keepRange; dx <= keepRange; ++dx)
                {
                    for (int dz = -keepRange; dz <= keepRange; ++dz)
                    { 
                        int gx = camGx + dx;
                        int gy = camGy + dy;
                        int gz = camGz + dz;

                        float nearX = Mathf.Clamp(camX, gx * GRID_SIZE, (gx + 1) * GRID_SIZE);
                        float nearZ = Mathf.Clamp(camZ, gz * GRID_SIZE, (gz + 1) * GRID_SIZE);
                        
                        float ddx = camX - nearX;
                        float ddz = camZ - nearZ;
                        float distSq = ddx * ddx + ddz * ddz;

                        if (distSq > unloadRadSq)
                        {
                            continue;
                        }

                        byte bX = (byte)(sbyte)gx;
                        byte bY = (byte)(sbyte)gy;
                        byte bZ = (byte)(sbyte)gz;
                        int targetGridKey = (bX << 16) | (bY << 8) | bZ;

                        if (_mapProvider.IsInvalidGrid(targetGridKey))
                        {
                            continue;
                        }

                        _keepGrids.Add(targetGridKey);

                        if (distSq > preloadRadSq)
                        {
                            continue;
                        }
                        if (_activeGrids.Contains(targetGridKey)
                            || !_loadingGrids.Add(targetGridKey))
                        {
                            continue;
                        }
                        if (!_validGridKeys.Contains(targetGridKey))
                        {
                            _loadingGrids.Remove(targetGridKey);
                            continue;
                        }

                        _ = LoadAndSpawnGridAsync(targetGridKey);
                    }
                }
            }

            _gridsToRemove.Clear();
            foreach (int loadedGridKey in _activeGrids)
            {
                if (!_keepGrids.Contains(loadedGridKey))
                {
                    _gridsToRemove.Add(loadedGridKey);
                }
            }

            for (int i = 0; i < _gridsToRemove.Count; ++i)
            {
                UnloadAndDestroyGrid(_gridsToRemove[i]);
            }
        }
        private async Awaitable LoadAndSpawnGridAsync(int gridKey)
        {
            try
            {
                MapGridData gridData = await _mapProvider.LoadGridDataAsync(gridKey);

                // DisposeAll() 호출했는지 검증
                if (!_loadingGrids.Contains(gridKey))
                {
                    if (null != gridData)
                    {
                        _mapProvider.UnloadGridData(gridKey);
                    }

                    return;
                }
                if (null != gridData
                    && null != gridData.layerMeshAssets)
                {
                    if (!_spawnedMapObjects.ContainsKey(gridKey))
                    {
                        _spawnedMapObjects[gridKey] = new List<MapChunk>();
                    }

                    for (int i = 0; i < gridData.layerMeshAssets.Count; ++i)
                    {
                        bool success = await CreateMapChunksAsync(gridKey, gridData.layerMeshAssets[i]);
                        if (!success)
                        {
                            _mapProvider.UnloadGridData(gridKey);
                            return;
                        }
                    }
                }

                if (_loadingGrids.Contains(gridKey))
                {
                    _activeGrids.Add(gridKey);
                }
            }
            catch (System.Exception e)
            {
                InLog.LogWarning($"[MapManager] Grid {gridKey} 로드 중 오류: {e.Message}");
            }
            finally
            {
                _loadingGrids.Remove(gridKey);
            }
        }
        private void UnloadAndDestroyGrid(int gridKey)
        {
            if (_spawnedMapObjects.TryGetValue(gridKey, out List<MapChunk> chunk))
            {
                for (int i = 0; i < chunk.Count; ++i)
                {
                    if (chunk[i].Obj)
                    {
                        Object.Destroy(chunk[i].Obj);
                    }
                }

                _spawnedMapObjects.Remove(gridKey);
            }

            _mapProvider.UnloadGridData(gridKey);
            _activeGrids.Remove(gridKey);
        }
        private async Awaitable<bool> CreateMapChunksAsync(int gridKey, MapGridLayerData layerData)
        {
            // 한 번에 생성하는 청크 개수를 제한;
            // n개 이상 청크 생성을 하면 다음 프레임으로 이어감;
            int instantiateCounter = 0;

            string meshAddress, matAddress;
            Mesh bakedMesh;
            MeshFilter filter;
            MeshRenderer renderer;
            Material mat;
            GameObject chunkObj;

            for (int i = 0; i < layerData.assets.Count; ++i)
            {
                meshAddress = layerData.assets[i];
                bakedMesh = await AssetProvider.LoadAssetAsync<Mesh>(meshAddress);
                if (!bakedMesh)
                {
                    continue;
                }

                matAddress = GetMaterialAddress(meshAddress);
                mat = await AssetProvider.LoadAssetAsync<Material>(matAddress);

                if (!_loadingGrids.Contains(gridKey))
                {
                    return false;
                }

                chunkObj = new GameObject(meshAddress);
                chunkObj.transform.SetParent(_rootTransform);
                chunkObj.transform.position = Vector3.zero;

                filter = chunkObj.AddComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;

                renderer = chunkObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat ? mat : new Material(Shader.Find("Standard"));

                MapChunk chunk = new MapChunk()
                {
                    Layer = layerData.layer,
                    Obj = chunkObj,
                    Renderer = renderer,
                    CurrentColor = Color.white
                };

                if (_spawnedMapObjects.TryGetValue(gridKey, out List<MapChunk> chunkList))
                {
                    chunkList.Add(chunk);
                }
                else
                {
                    Object.Destroy(chunkObj);   
                    return false;
                }

                ++instantiateCounter;
                if (0 == instantiateCounter % 3)
                {
                    await Awaitable.NextFrameAsync();
                    if (!_loadingGrids.Contains(gridKey))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        // --- 레이어 시각적 제어 ---
        public async Awaitable UpdateLayerFormTileAsync(float3 playerWorldPos, float fadeDuration = 1.0f)
        {
            if (!_mapProvider.TryGetTileData(in playerWorldPos, out MapTileData tileData))
            {
                return;
            }

            ushort newLayerMask = tileData.LayerMask;
            if (_lastLayerMask == newLayerMask)
            {
                return;
            }

            _lastLayerMask = newLayerMask;
            await UpdateLayerVisibilityAsync(newLayerMask, false, fadeDuration);
        }
        private async Awaitable UpdateLayerVisibilityAsync(int currentLayer, bool hideInsteadOfDim = false, float duration = 1.0f)
        {
            ++_layerTransitionToken;
            int currentToken = _layerTransitionToken;

            Color normalColor = Color.white;
            Color dimColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            Color hideColor = new Color(0f, 0f, 0f, 0f);

            _animatingChunksCache.Clear();

            foreach (var kvp in _spawnedMapObjects)
            {
                List<MapChunk> gridChunks = kvp.Value;

                MapChunk chunk;
                for (int i = 0; i < gridChunks.Count; ++i)
                {
                    chunk = gridChunks[i];
                    chunk.StartColor = chunk.CurrentColor;

                    if (currentLayer == chunk.Layer)
                    {
                        chunk.TargetColor = normalColor;
                        if (!chunk.Renderer.enabled)
                        {
                            chunk.Renderer.enabled = true;
                        }
                    }
                    else
                    {
                        chunk.TargetColor = hideInsteadOfDim ? hideColor : dimColor;
                        if (!chunk.Renderer.enabled
                            && !hideInsteadOfDim)
                        {
                            chunk.Renderer.enabled = true;
                        }
                    }

                    _animatingChunksCache.Add(chunk);
                }
            }


            float elapsed = 0f;
            float duration_recip = 1f / duration;

            while (elapsed < duration)
            {
                if (currentToken != _layerTransitionToken)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed * duration_recip);

                MapChunk chunk;
                for (int i = 0; i < _animatingChunksCache.Count; ++i)
                {
                    chunk = _animatingChunksCache[i];
                    chunk.CurrentColor = chunk.TargetColor;

                    chunk.Renderer.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                    chunk.Renderer.SetPropertyBlock(_propBlock);
                }

                await Awaitable.NextFrameAsync();
            }

            if (_layerTransitionToken != currentToken)
            {
                return;
            }

            for (int i = 0; i < _animatingChunksCache.Count; i++)
            {
                MapChunk chunk = _animatingChunksCache[i];
                chunk.CurrentColor = chunk.TargetColor;

                chunk.Renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                chunk.Renderer.SetPropertyBlock(_propBlock);

                if (chunk.Layer != currentLayer && hideInsteadOfDim) chunk.Renderer.enabled = false;
            }

            _animatingChunksCache.Clear();
        }
        //*/

        // --- 유틸리티 ---
        private string GetMaterialAddress(string meshName)
        {
            if (_materialAddressCache.TryGetValue(meshName, out string cachedMatAddress)) return cachedMatAddress;

            int lastUnderScore = meshName.LastIndexOf('_');
            if (lastUnderScore == -1) return "Mat_Default";

            int prefixEndIndex = -1;
            int underscoreCount = 0;

            for (int i = 0; i < meshName.Length; i++)
            {
                if ('_' == meshName[i])
                {
                    underscoreCount++;
                    if (4 == underscoreCount)
                    {
                        prefixEndIndex = i;
                        break;
                    }
                }
            }

            string resultMatAddress = "Mat_Default";
            if (prefixEndIndex != -1 && lastUnderScore > prefixEndIndex)
            {
                string atlases = meshName.Substring(prefixEndIndex + 1, lastUnderScore - prefixEndIndex - 1);
                resultMatAddress = $"Mat_{atlases}";
            }

            _materialAddressCache[meshName] = resultMatAddress;
            return resultMatAddress;
        }

        public override void OnDisable()
        {
            DisposeAll();
        }
    }
}
