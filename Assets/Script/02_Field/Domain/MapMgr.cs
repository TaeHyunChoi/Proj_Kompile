namespace Kompile.Domain
{
    using Data;
    using UnityEngine;
    using Unity.Mathematics;
    using System;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary> 거리 기반 우선순위 큐 정렬을 위한 구조체 (GC Alloc 방지) </summary>
    public struct GridLoadRequest : IComparable<GridLoadRequest>
    {
        public int Key;
        public float DistSq;

        public int CompareTo(GridLoadRequest other) => DistSq.CompareTo(other.DistSq);
    }

    /// <summary> 맵 그리드 (로드) 상태 </summary>
    public enum EGridState : byte
    {
        Queued = 0,
        Loading,
        Active
    }

    /// <summary> 맵 오브젝트 인스턴스 스폰, 시각적 트랜지션, 큐 기반 동기적 스트리밍 제어 (Instance-Centric) </summary>
    public class MapMgr : GameLogicMgrBase
    {
        private const float PRELOAD_RADIUS = 10f;
        private const float UNLOAD_RADIUS = 20f;
        private const float CHECK_INTERVAL = 1f;
        private const float GRID_SIZE = 64f;
        private const float GRID_SIZE_RECIP = 1f / 64f;
        private const float UNLOAD_RAD_SQUARE = UNLOAD_RADIUS * UNLOAD_RADIUS;
        private const float PRELOAD_RAD_SQUARE = PRELOAD_RADIUS * PRELOAD_RADIUS;
        private const int MAX_CONCURRENT_LOADS = 2;


        private static readonly int KEEP_RANGE = Mathf.CeilToInt(UNLOAD_RADIUS * GRID_SIZE_RECIP) + 1;
        private static readonly int Y_RANGE = Mathf.CeilToInt(GRID_SIZE * GRID_SIZE_RECIP);
        private static readonly int COLOR_PROP_ID = Shader.PropertyToID("_Color");


        // --- Container : Map grid, Map chunk ---
        private readonly Queue<MapChunk> _chunkPool = new Queue<MapChunk>();
        private readonly Dictionary<int, List<MapChunk>> _spawnedMapObjects = new Dictionary<int, List<MapChunk>>();
        private readonly Dictionary<int, EGridState> _gridLoadStatuses = new Dictionary<int, EGridState>();
        private readonly List<(int key, CancellationTokenSource ctk)> _loadingTasks = new List<(int, CancellationTokenSource)>(MAX_CONCURRENT_LOADS);


        // --- Container : Buffer (GC 방지) ---
        private readonly HashSet<int> _keepGrids = new HashSet<int>();
        private readonly List<GridLoadRequest> _loadList = new List<GridLoadRequest>();
        private readonly List<int> _obsoleteGrids = new List<int>();

        // 따라서 청크 A가 '풀 매터리얼'을 쓰고 청크 B가 '바위 매터리얼'을 쓰더라도,
        // 두 매터리얼 셰이더 내부에 _Color라는 속성(Property)만 존재한다면 각자의 텍스처와 질감을 유지한 채
        // 투명도만 완벽하게 제어됩니다. 매터리얼 복제(가비지)도 전혀 발생하지 않습니다.
        private readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();


        // --- Field ---
        private HashSet<int> _validGridKeys;  // 할당(new) 없이 외부 데이터의 참조만 보관
        private MapProvider _mapProvider;
        private Transform _rootTransform;
        private Material _fallbackMaterial;
        private int _layerTransitionToken = 0;
        private ushort _lastLayerMask = ushort.MaxValue;
        private int _currentLoadingCount = 0;
        private bool _isStreamingActive = false;
        private float _streamTimer = CHECK_INTERVAL;


        // --- Getter ---
        public MapProvider Provider => _mapProvider;


        // --- Initialize ---
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

            Shader stdShader = Shader.Find("Standard");
            if (null != stdShader)
            {
                _fallbackMaterial = new Material(stdShader);
            }

            return true;
        }
#pragma warning restore 1998


        // --- Update ---
        public override async Awaitable<bool> OnUpdate()
        {
            if (!_isStreamingActive)
            {
                return false;
            }

            _streamTimer += Time.deltaTime;
            bool update = await ProcessRequests();
            // 필요 시 추가;

            return update;
        }
        protected override async Awaitable<bool> HandleRequestAsync(RequestBase request)
        {
            switch (request.Type)
            {
                case RequestType.Map_Update:
                    var req = request as MapUpdateRequest;
                    float3 pos = req.Position;
                    if (_streamTimer >= CHECK_INTERVAL)
                    {
                        _streamTimer = 0f;
                        CheckAndTriggerStreaming(pos);
                    }

                    _ = UpdateLayerFromTileAsync(pos);
                    break;
                default:
                    break;
            }

            request.ReturnToPool();
            return true;
        }


        // --- Map Streaming : Grid (Sync) ---
        public void PlayStreaming(HashSet<int> validGridKeys)
        {
            _isStreamingActive = true;
            _streamTimer = CHECK_INTERVAL;
            _validGridKeys = validGridKeys;
        }
        private void CheckAndTriggerStreaming(float3 playerPos)
        {
            float px = playerPos.x;
            float py = playerPos.y;
            float pz = playerPos.z;

            int pgx = Mathf.FloorToInt(px * GRID_SIZE_RECIP);
            int pgy = Mathf.FloorToInt(py * GRID_SIZE_RECIP);
            int pgz = Mathf.FloorToInt(pz * GRID_SIZE_RECIP);

            _keepGrids.Clear();

            for (int dy = -Y_RANGE; dy <= Y_RANGE; ++dy)
            {
                for (int dx = -KEEP_RANGE; dx <= KEEP_RANGE; ++dx)
                {
                    for (int dz = -KEEP_RANGE; dz <= KEEP_RANGE; ++dz)
                    {
                        int gx = pgx + dx;
                        int gy = pgy + dy;
                        int gz = pgz + dz;

                        float nearX = Mathf.Clamp(px, gx * GRID_SIZE, (gx + 1) * GRID_SIZE);
                        float nearZ = Mathf.Clamp(pz, gz * GRID_SIZE, (gz + 1) * GRID_SIZE);

                        float ddx = px - nearX;
                        float ddz = pz - nearZ;
                        float distSq = ddx * ddx + ddz * ddz;

                        // check dist: unload ( 가장 먼 범위 )
                        if (distSq > UNLOAD_RAD_SQUARE)
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


                        // check dist: pre-load ( 그 다음으로 먼 범위 )
                        if (distSq > PRELOAD_RAD_SQUARE)
                        {
                            continue;
                        }
                        if (_validGridKeys != null && !_validGridKeys.Contains(targetGridKey))
                        {
                            continue;
                        }
                        if (_gridLoadStatuses.ContainsKey(targetGridKey))
                        {
                            continue;
                        }

                        _gridLoadStatuses[targetGridKey] = EGridState.Queued;
                        _loadList.Add(new GridLoadRequest 
                        {
                            Key = targetGridKey, 
                            DistSq = distSq 
                        });
                    }
                }
            }

            // 플레이어가 너무 빠르게 이동해서 로딩 대기(Queued) 중이던 그리드가
            // 유지 반경 밖으로 다시 벗어나는엣지 케이스를 방어
            for (int i = _loadList.Count - 1; i >= 0; --i)
            {
                if (!_keepGrids.Contains(_loadList[i].Key))
                {
                    _gridLoadStatuses.Remove(_loadList[i].Key);
                    _loadList.RemoveAt(i);
                }
            }

            // 플레이어 위치로부터 가까운 순으로 정렬 후 Process Load;
            _loadList.Sort();
            TryProcessLoadQueue();

            // 범위를 아예 벗어난 그리드 정리;
            _obsoleteGrids.Clear();
            foreach (var kvp in _gridLoadStatuses)
            {
                if (!_keepGrids.Contains(kvp.Key))
                {
                    _obsoleteGrids.Add(kvp.Key);
                }
            }
            for (int i = 0; i < _obsoleteGrids.Count; ++i)
            {
                CleanupGridState(_obsoleteGrids[i]);
            }
        }
        public void StopStreaming()
        {
            _isStreamingActive = false;
        }


        // --- Map Streaming : Layer ---
        public async Awaitable<bool> UpdateLayerFromTileAsync(float3 playerWorldPos, float fadeDuration = 0.125f)
        {
            if (!_mapProvider.TryGetTileData(in playerWorldPos, out MapTileData tileData))
            {
                return false;
            }

            ushort newLayerMask = tileData.LayerMask;
            if (_lastLayerMask == newLayerMask)
            {
                return false;
            }

            _lastLayerMask = newLayerMask;
            await UpdateLayerVisibilityAsync(newLayerMask, false, fadeDuration);
            return true;
        }
        private async Awaitable UpdateLayerVisibilityAsync(int currentLayer, bool hideInsteadOfDim = false, float duration = 0.125f)
        {
            ++_layerTransitionToken;
            int currentToken = _layerTransitionToken;

            Color normalColor = Color.white;
            Color dimColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            Color hideColor = new Color(0f, 0f, 0f, 0f);

            foreach (var kvp in _spawnedMapObjects)
            {
                List<MapChunk> gridChunks = kvp.Value;
                for (int i = 0; i < gridChunks.Count; ++i)
                {
                    MapChunk chunk = gridChunks[i];
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

                foreach (var kvp in _spawnedMapObjects)
                {
                    List<MapChunk> gridChunks = kvp.Value;
                    for (int i = 0; i < gridChunks.Count; ++i)
                    {
                        MapChunk chunk = gridChunks[i];

                        chunk.CurrentColor = Color.Lerp(chunk.StartColor, chunk.TargetColor, t);
                        chunk.Renderer.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(COLOR_PROP_ID, chunk.CurrentColor);
                        chunk.Renderer.SetPropertyBlock(_propBlock);
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            if (_layerTransitionToken != currentToken)
            {
                return;
            }

            foreach (var kvp in _spawnedMapObjects)
            {
                List<MapChunk> gridChunks = kvp.Value;
                for (int i = 0; i < gridChunks.Count; ++i)
                {
                    MapChunk chunk = gridChunks[i];
                    chunk.CurrentColor = chunk.TargetColor;

                    chunk.Renderer.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(COLOR_PROP_ID, chunk.CurrentColor);
                    chunk.Renderer.SetPropertyBlock(_propBlock);

                    if (chunk.Layer != currentLayer && hideInsteadOfDim)
                    {
                        chunk.Renderer.enabled = false;
                    }
                }
            }
        }


        // --- Streaming Helper ---
        private async Awaitable LoadAndSpawnGridAsync(int gridKey, CancellationToken token)
        {
            try
            {
                MapGridData gridData = await _mapProvider.LoadGridDataAsync(gridKey);

                if (token.IsCancellationRequested)
                {
                    if (gridData != null)
                    {
                        _mapProvider.UnloadGridData(gridKey);
                    }

                    return;
                }

                if (gridData != null && gridData.layerMeshAssets != null)
                {
                    if (!_spawnedMapObjects.ContainsKey(gridKey))
                    {
                        _spawnedMapObjects[gridKey] = new List<MapChunk>();
                    }

                    bool success = await CreateMapChunksAsync(gridKey, gridData, token);
                    if (!success || token.IsCancellationRequested)
                    {
                        CleanupGridState(gridKey);
                        return;
                    }
                }

                if (!token.IsCancellationRequested && _keepGrids.Contains(gridKey))
                {
                    _gridLoadStatuses[gridKey] = EGridState.Active;
                }
                else
                {
                    CleanupGridState(gridKey);
                }
            }
            catch (OperationCanceledException)
            {
                CleanupGridState(gridKey);
            }
            catch (Exception e)
            {
                InLog.LogWarning($"[MapManager] Grid {gridKey} 로드 중 오류: {e.Message}");
                CleanupGridState(gridKey);
            }
            finally
            {
                for (int i = _loadingTasks.Count - 1; i >= 0; --i)
                {
                    if (_loadingTasks[i].key == gridKey && _loadingTasks[i].ctk.Token == token)
                    {
                        _loadingTasks[i].ctk.Dispose();
                        _loadingTasks.RemoveAt(i);
                        break;
                    }
                }

                _currentLoadingCount--;
                TryProcessLoadQueue();
            }
        }
        private void TryProcessLoadQueue()
        {
            while (_currentLoadingCount < MAX_CONCURRENT_LOADS 
                   && _loadList.Count > 0)
            {
                GridLoadRequest req = _loadList[0];
                _loadList.RemoveAt(0);

                if (!_keepGrids.Contains(req.Key))
                {
                    _gridLoadStatuses.Remove(req.Key);
                    continue;
                }

                if (_gridLoadStatuses.TryGetValue(req.Key, out EGridState state) && state != EGridState.Queued)
                {
                    continue;
                }

                _gridLoadStatuses[req.Key] = EGridState.Loading;

                CancellationTokenSource cts = new CancellationTokenSource();

                _loadingTasks.Add((req.Key, cts));
                _currentLoadingCount++;

                _ = LoadAndSpawnGridAsync(req.Key, cts.Token);
            }
        }
        private async Awaitable<bool> CreateMapChunksAsync(int gridKey, MapGridData gridData, CancellationToken token)
        {
            int instantiateCounter = 0;

            string meshAddress, matAddress;
            Mesh bakedMesh;
            Material mat;

            for (int i = 0; i < gridData.layerMeshAssets.Count; ++i)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                meshAddress = gridData.layerMeshAssets[i].assets[0];
                bakedMesh = await AssetProvider.LoadAssetAsync<Mesh>(meshAddress);
                if (!bakedMesh) continue;

                if (token.IsCancellationRequested)
                {
                    return false;
                }

                matAddress = AssetProvider.GetMaterialAddress(meshAddress);
                mat = await AssetProvider.LoadAssetAsync<Material>(matAddress);

                if (token.IsCancellationRequested)
                {
                    return false;
                }

                MapChunk chunk = GetOrCreateChunkFromPool();
                chunk.Obj.name = meshAddress;
                chunk.Obj.transform.position = Vector3.zero;

                MeshFilter filter = chunk.Obj.GetComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;

                chunk.Renderer.sharedMaterial = mat ? mat : _fallbackMaterial;
                chunk.Layer = gridData.layerMeshAssets[i].layer;
                chunk.CurrentColor = Color.white;

                if (_spawnedMapObjects.TryGetValue(gridKey, out List<MapChunk> chunkList))
                {
                    chunkList.Add(chunk);
                }
                else
                {
                    chunk.Obj.SetActive(false);
                    chunk.Obj.GetComponent<MeshFilter>().sharedMesh = null;
                    chunk.Renderer.sharedMaterial = null;
                    _chunkPool.Enqueue(chunk);

                    return false;
                }

                ++instantiateCounter;
                if (0 == instantiateCounter % 3)
                {
                    await Awaitable.NextFrameAsync();
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        private MapChunk GetOrCreateChunkFromPool()
        {
            if (_chunkPool.Count > 0)
            {
                MapChunk pooledChunk = _chunkPool.Dequeue();
                pooledChunk.Obj.SetActive(true);
                return pooledChunk;
            }

            GameObject obj = new GameObject("PooledMapChunk");
            obj.transform.SetParent(_rootTransform);

            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

            return new MapChunk()
            {
                Obj = obj,
                Renderer = renderer
            };
        }
        private void CleanupGridState(int gridKey)
        {
            for (int i = _loadingTasks.Count - 1; i >= 0; --i)
            {
                if (_loadingTasks[i].key == gridKey)
                {
                    _loadingTasks[i].ctk.Cancel();
                    break;
                }
            }

            if (_spawnedMapObjects.TryGetValue(gridKey, out List<MapChunk> chunkList))
            {
                for (int i = 0; i < chunkList.Count; ++i)
                {
                    if (chunkList[i].Obj)
                    {
                        chunkList[i].Obj.SetActive(false);

                        MeshFilter filter = chunkList[i].Obj.GetComponent<MeshFilter>();
                        if (filter)
                        {
                            filter.sharedMesh = null;
                        }
                        if (chunkList[i].Renderer)
                        {
                            chunkList[i].Renderer.sharedMaterial = null;
                        }

                        _chunkPool.Enqueue(chunkList[i]);
                    }
                }
                chunkList.Clear();
                _spawnedMapObjects.Remove(gridKey);
            }

            _mapProvider.UnloadGridData(gridKey);
            _gridLoadStatuses.Remove(gridKey);
        }


        // --- Disable ---
        public override void OnDisable()
        {
            StopStreaming();

            for (int i = 0; i < _loadingTasks.Count; ++i)
            {
                _loadingTasks[i].ctk.Cancel();
            }
            _loadingTasks.Clear();
            _currentLoadingCount = 0;

            foreach (var kvp in _spawnedMapObjects)
            {
                List<MapChunk> chunks = kvp.Value;
                for (int i = 0; i < chunks.Count; ++i)
                {
                    if (chunks[i].Obj)
                    {
                        UnityEngine.Object.Destroy(chunks[i].Obj);
                    }
                }
            }

            while (_chunkPool.Count > 0)
            {
                MapChunk chunk = _chunkPool.Dequeue();
                if (chunk.Obj)
                {
                    UnityEngine.Object.Destroy(chunk.Obj);
                }
            }

            _spawnedMapObjects.Clear();
            _gridLoadStatuses.Clear();
            _obsoleteGrids.Clear();
            _keepGrids.Clear();
            _loadList.Clear();

            _validGridKeys = null;

            if (_fallbackMaterial != null)
            {
                UnityEngine.Object.Destroy(_fallbackMaterial);
                _fallbackMaterial = null;
            }

            if (null != _mapProvider)
            {
                _mapProvider.Dispose();
            }
        }
    }
}