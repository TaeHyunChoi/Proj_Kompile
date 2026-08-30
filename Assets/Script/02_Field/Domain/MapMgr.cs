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

        /// <summary>
        /// CompareTo를 미리 정의해둔 것입니다. 이렇게 하면 _loadList.Sort(); 단 한 줄만 호출해도 
        /// 가비지 생성(0 bytes) 없이 완벽하고 빠르게 가장 가까운 거리순으로 정렬이 이루어집니다. 
        /// 겉으로는 안 보이지만 보이지 않는 곳에서 제일 열심히 일하고 있는 녀석입니다!
        /// </summary>
        public int CompareTo(GridLoadRequest other) => DistSq.CompareTo(other.DistSq);
    }

    /// <summary> 맵 오브젝트 인스턴스 스폰, 시각적 트랜지션, 큐 기반 동기적 스트리밍 제어 (Instance-Centric) </summary>
    public class MapMgr : GameLogicMgrBase
    {
        private const float PRELOAD_RADIUS = 10f;
        private const float UNLOAD_RADIUS = 20f;
        private const float CHECK_INTERVAL = 1f;
        private const float GRID_SIZE = 64f;
        private const float GRID_SIZE_RECIP = 1f / 64f;

        // --- 동시 로딩 제어 ---
        private const int MAX_CONCURRENT_LOADS = 2;
        private int _currentLoadingCount = 0;

        private MapProvider _mapProvider;
        private Transform _rootTransform;

        // 캐싱된 대체 매터리얼 (메인 스레드 스파이크 방지용)
        private Material _fallbackMaterial;

        // --- 로컬 오브젝트 풀 ---
        // 생성 및 파괴 스파이크를 없애기 위해 다 쓴 청크를 보관하는 큐
        private readonly Queue<MapChunk> _chunkPool = new Queue<MapChunk>();

        private readonly Dictionary<int, List<MapChunk>> _spawnedMapObjects = new Dictionary<int, List<MapChunk>>();
        private readonly HashSet<int> _activeGrids = new HashSet<int>();

        // 상태/대기열 관리 컬렉션
        private readonly HashSet<int> _keepGrids = new HashSet<int>();
        private readonly HashSet<int> _queuedGrids = new HashSet<int>();
        private readonly List<GridLoadRequest> _loadList = new List<GridLoadRequest>();
        private readonly Dictionary<int, CancellationTokenSource> _loadingTasks = new Dictionary<int, CancellationTokenSource>();

        private readonly List<int> _gridsToRemove = new List<int>();
        private readonly List<int> _tasksToCancel = new List<int>();

        private HashSet<int> _validGridKeys = new HashSet<int>();
        private bool _isStreamingActive = false;
        private float _streamTimer = CHECK_INTERVAL;

        private Transform CameraTransform => InCamera.Main.transform;
        public MapProvider Provider => _mapProvider;


        // --- Optimization Caches ---
        private readonly List<MapChunk> _animatingChunksCache = new List<MapChunk>();

        // --- Rendering & Visuals ---
        private readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();
        private static readonly int ColorPropID = Shader.PropertyToID("_Color");
        private int _layerTransitionToken = 0;
        private ushort _lastLayerMask = ushort.MaxValue;


        // --- Camera & Streaming Config ---
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

            // 로딩 스파이크 방지를 위해 시스템 초기화 시점에 1회 셰이더 검색 및 캐싱
            Shader stdShader = Shader.Find("Standard");
            if (stdShader != null)
            {
                _fallbackMaterial = new Material(stdShader);
            }

            return true;
        }
#pragma warning restore 1998

        public override async Awaitable<bool> OnUpdate()
        {
            if (!_isStreamingActive)
            {
                return false;
            }

            _streamTimer += Time.deltaTime;
            bool update = await ProcessRequests();

            return update;
        }

        protected override async Awaitable<bool> HandleRequestAsync(RequestBase request)
        {
            await Awaitable.NextFrameAsync();

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

                    _ = UpdateLayerFormTileAsync(pos);
                    break;
                default:
                    break;
            }

            request.ReturnToPool();
            return true;
        }


        // --- System Control ---
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

            // 진행 중인 모든 로딩 작업 강제 취소
            foreach (var kvp in _loadingTasks)
            {
                kvp.Value.Cancel();
                // [안전장치] Dispose는 각 태스크의 finally 블록에게 온전히 위임하여 ObjectDisposedException 예외 방지
            }
            _loadingTasks.Clear();
            _currentLoadingCount = 0;

            List<MapChunk> chunks;
            foreach (var kvp in _spawnedMapObjects)
            {
                chunks = kvp.Value;
                for (int i = 0; i < chunks.Count; ++i)
                {
                    if (chunks[i].Obj) UnityEngine.Object.Destroy(chunks[i].Obj);
                }
            }

            // 풀에 보관된 청크들도 모두 파괴하여 메모리 완전 해제
            while (_chunkPool.Count > 0)
            {
                MapChunk chunk = _chunkPool.Dequeue();
                if (chunk.Obj) UnityEngine.Object.Destroy(chunk.Obj);
            }

            _spawnedMapObjects.Clear();
            _activeGrids.Clear();
            _gridsToRemove.Clear();
            _keepGrids.Clear();
            _validGridKeys.Clear();
            _animatingChunksCache.Clear();
            _loadList.Clear();
            _queuedGrids.Clear();
            _tasksToCancel.Clear();

            // 생성해둔 대체 매터리얼 메모리 해제
            if (_fallbackMaterial != null)
            {
                UnityEngine.Object.Destroy(_fallbackMaterial);
                _fallbackMaterial = null;
            }

            _mapProvider?.Dispose();
        }


        // --- Streaming (sync) ---
        private void CheckAndTriggerStreaming(float3 playerPos)
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

            float px = playerPos.x;
            float py = playerPos.y;
            float pz = playerPos.z;

            int pgx = Mathf.FloorToInt(px * GRID_SIZE_RECIP);
            int pgy = Mathf.FloorToInt(py * GRID_SIZE_RECIP);
            int pgz = Mathf.FloorToInt(pz * GRID_SIZE_RECIP);

            _keepGrids.Clear();

            for (int dy = -yRange; dy <= yRange; ++dy)
            {
                for (int dx = -keepRange; dx <= keepRange; ++dx)
                {
                    for (int dz = -keepRange; dz <= keepRange; ++dz)
                    {
                        int gx = pgx + dx;
                        int gy = pgy + dy;
                        int gz = pgz + dz;

                        float nearX = Mathf.Clamp(px, gx * GRID_SIZE, (gx + 1) * GRID_SIZE);
                        float nearZ = Mathf.Clamp(pz, gz * GRID_SIZE, (gz + 1) * GRID_SIZE);

                        float ddx = px - nearX;
                        float ddz = pz - nearZ;
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

                        if (!_validGridKeys.Contains(targetGridKey))
                        {
                            continue;
                        }

                        // 큐 수집: 이미 활성화/로딩/대기 중이면 패스
                        if (_activeGrids.Contains(targetGridKey) ||
                            _loadingTasks.ContainsKey(targetGridKey) ||
                            _queuedGrids.Contains(targetGridKey))
                        {
                            continue;
                        }

                        _loadList.Add(new GridLoadRequest { Key = targetGridKey, DistSq = distSq });
                        _queuedGrids.Add(targetGridKey);
                    }
                }
            }

            // 대기열 가지치기 (기다리던 중 멀어진 그리드 제외)
            for (int i = _loadList.Count - 1; i >= 0; --i)
            {
                if (!_keepGrids.Contains(_loadList[i].Key))
                {
                    _queuedGrids.Remove(_loadList[i].Key);
                    _loadList.RemoveAt(i);
                }
            }

            // 거리 우선순위 정렬 및 로드 큐 가동
            _loadList.Sort();
            TryProcessLoadQueue();

            // 1. 활성화된 그리드 언로드
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
                CleanupGridState(_gridsToRemove[i]);
            }

            // 2. 현재 로딩 중인 태스크가 범위를 벗어났다면 '취소 신호' 전송
            _tasksToCancel.Clear();
            foreach (var kvp in _loadingTasks)
            {
                if (!_keepGrids.Contains(kvp.Key))
                {
                    _tasksToCancel.Add(kvp.Key);
                }
            }
            for (int i = 0; i < _tasksToCancel.Count; ++i)
            {
                _loadingTasks[_tasksToCancel[i]].Cancel();
            }
        }

        private void TryProcessLoadQueue()
        {
            while (_currentLoadingCount < MAX_CONCURRENT_LOADS && _loadList.Count > 0)
            {
                GridLoadRequest req = _loadList[0];
                _loadList.RemoveAt(0);
                _queuedGrids.Remove(req.Key);

                if (!_keepGrids.Contains(req.Key) || _activeGrids.Contains(req.Key) || _loadingTasks.ContainsKey(req.Key))
                {
                    continue;
                }

                CancellationTokenSource cts = new CancellationTokenSource();
                _loadingTasks.Add(req.Key, cts);
                _currentLoadingCount++;

                _ = LoadAndSpawnGridAsync(req.Key, cts.Token);
            }
        }

        private async Awaitable LoadAndSpawnGridAsync(int gridKey, CancellationToken token)
        {
            try
            {
                MapGridData gridData = await _mapProvider.LoadGridDataAsync(gridKey);

                if (token.IsCancellationRequested)
                {
                    if (gridData != null) _mapProvider.UnloadGridData(gridKey);
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
                    _activeGrids.Add(gridKey);
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
                // Task가 완전히 종료되는 이 지점에서만 안전하게 Dispose
                if (_loadingTasks.TryGetValue(gridKey, out var cts) && cts.Token == token)
                {
                    cts.Dispose();
                    _loadingTasks.Remove(gridKey);
                }

                _currentLoadingCount--;
                TryProcessLoadQueue();
            }
        }

        // --- 풀에서 꺼내오거나 생성하는 헬퍼 메서드 ---
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

        private async Awaitable<bool> CreateMapChunksAsync(int gridKey, MapGridData gridData, CancellationToken token)
        {
            int instantiateCounter = 0;

            string meshAddress, matAddress;
            Mesh bakedMesh;
            Material mat;

            for (int i = 0; i < gridData.layerMeshAssets.Count; ++i)
            {
                // 중간에 멀어지면 생성 중단
                if (token.IsCancellationRequested) return false;

                // [유의] AssetProvider 내부에 비동기 대기가 포함되어 있으므로 이 시점에도 취소될 수 있습니다.
                meshAddress = gridData.layerMeshAssets[i].assets[0];
                bakedMesh = await AssetProvider.LoadAssetAsync<Mesh>(meshAddress);
                if (!bakedMesh) continue;

                if (token.IsCancellationRequested) return false;

                matAddress = AssetProvider.GetMaterialAddress(meshAddress);
                mat = await AssetProvider.LoadAssetAsync<Material>(matAddress);

                if (token.IsCancellationRequested) return false;

                // 오브젝트 생성(new GameObject) 부하를 없애고 풀링 시스템 적용
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
                    // 로딩 도중 그리드가 해제되어 리스트가 사라진 경우 객체를 다시 풀로 반납
                    chunk.Obj.SetActive(false);
                    chunk.Obj.GetComponent<MeshFilter>().sharedMesh = null;
                    chunk.Renderer.sharedMaterial = null;
                    _chunkPool.Enqueue(chunk);
                    return false;
                }

                // 프레임 분산 로직 (3개당 1번 프레임 대기)
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

        private void CleanupGridState(int gridKey)
        {
            // 로딩 중인 태스크가 있다면 취소 플래그 세팅 (Dispose는 비동기 함수가 끝날 때 스스로 함)
            if (_loadingTasks.TryGetValue(gridKey, out var cts))
            {
                cts.Cancel();
            }

            if (_spawnedMapObjects.TryGetValue(gridKey, out List<MapChunk> chunkList))
            {
                for (int i = 0; i < chunkList.Count; ++i)
                {
                    if (chunkList[i].Obj)
                    {
                        // Destroy 대신 비활성화하여 풀에 반환 (메모리 릭 방지를 위해 참조 제거)
                        chunkList[i].Obj.SetActive(false);

                        MeshFilter filter = chunkList[i].Obj.GetComponent<MeshFilter>();
                        if (filter) filter.sharedMesh = null;
                        if (chunkList[i].Renderer) chunkList[i].Renderer.sharedMaterial = null;

                        _chunkPool.Enqueue(chunkList[i]);
                    }
                }
                chunkList.Clear();
                _spawnedMapObjects.Remove(gridKey);
            }

            _mapProvider.UnloadGridData(gridKey);
            _activeGrids.Remove(gridKey);
            _queuedGrids.Remove(gridKey);
        }

        // --- Control Layer Visualization ---
        public async Awaitable<bool> UpdateLayerFormTileAsync(float3 playerWorldPos, float fadeDuration = 1.0f)
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

        public override void OnDisable()
        {
            DisposeAll();
        }
    }
}