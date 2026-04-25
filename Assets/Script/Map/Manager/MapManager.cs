namespace Kompile.Map.Manager
{
    using Kompile.Map.Utility;
    using Kompile.Map.Data;
    using Kompile.Asset.Provider;
    using UnityEngine;
    using System.Collections.Generic;
    using Unity.Mathematics;

    /// <summary> 인게임 맵 그리드의 동적 스트리밍, 레이어 시각적 제어, 실시간 인스턴스 관리를 전담 </summary>
    public class MapManager
    {
        // --- Manager State (Instance-Centric) ---
        private readonly Dictionary<int, MapGridData> _mapGridDataDic;
        private readonly Dictionary<int, List<MapChunkContext>> _spawnedMapObjects;
        private readonly Transform _rootTransform;

        // --- Streaming State ---
        private readonly HashSet<int> _activeGrids;     // 로드 완료된 그리드 키
        private readonly HashSet<int> _loadingGrids;    // 현재 로딩 프로세스 중인 그리드 키
        private readonly List<int> _gridsToRemove;   // 언로드 계산용 임시 리스트 (GC 방지)
        private readonly HashSet<int> _keepGrids;       // 언로드 방지용 임시 셋 (GC 방지)
        private readonly HashSet<int> _invalidGrids;    // 존재하지 않는 맵(팬텀 그리드) 블랙리스트 (GC 및 예외 방지)

        // --- Optimization Caches (GC 최소화) ---
        private readonly Dictionary<string, string> _materialAddressCache; // 매테리얼 주소 캐싱
        private readonly Dictionary<int, string> _gridKeyAddressCache;     // Addressables 키 문자열 캐싱
        private readonly List<MapChunkContext> _animatingChunksCache;      // 애니메이션 루프용 1차원 평탄화 리스트

        // --- Rendering & Visuals ---
        private readonly MaterialPropertyBlock _propBlock;
        private static readonly int ColorPropID = Shader.PropertyToID("_Color");
        private int _layerTransitionToken = 0; // 레이어 전환 애니메이션 중첩 방지 토큰

        // --- Camera & Streaming Config ---
        private Transform _cameraTransform;
        private bool _isStreamingActive = false;

        private const float PRELOAD_RADIUS = 10f;  // 로드 시작 반경 (Far 5 + 여유 5)
        private const float UNLOAD_RADIUS = 20f;  // 언로드 시작 반경 (Preload + 10)
        private const float CHECK_INTERVAL = 0.5f; // 스트리밍 검사 주기 (초)

        public MapManager(Transform root)
        {
            _mapGridDataDic = new Dictionary<int, MapGridData>();
            _spawnedMapObjects = new Dictionary<int, List<MapChunkContext>>();
            _activeGrids = new HashSet<int>();
            _loadingGrids = new HashSet<int>();
            _gridsToRemove = new List<int>(16);
            _keepGrids = new HashSet<int>();
            _invalidGrids = new HashSet<int>();

            _materialAddressCache = new Dictionary<string, string>();
            _gridKeyAddressCache = new Dictionary<int, string>();
            _animatingChunksCache = new List<MapChunkContext>(128);

            _rootTransform = root;
            _propBlock = new MaterialPropertyBlock();
        }

        // ===================================================================================
        // 시스템 제어 인터페이스
        // ===================================================================================

        public async Awaitable PlayStreamingAsync(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
            _isStreamingActive = true;

            // 배경 스트리밍 루프 시작
            await StartGridStreamingLoopAsync();
        }

        public void StopStreaming()
        {
            _isStreamingActive = false;
        }

        /// <summary>
        /// 로드된 모든 그리드·청크 오브젝트를 즉시 해제하고 내부 상태를 초기화합니다.
        /// StopStreaming() 호출 이후에 사용하십시오.
        /// </summary>
        public void DisposeAll()
        {
            // 활성화된 청크 오브젝트 전체 파괴
            foreach (var kvp in _spawnedMapObjects)
            {
                List<MapChunkContext> chunks = kvp.Value;
                for (int i = 0; i < chunks.Count; ++i)
                {
                    if (chunks[i].Obj)
                    {
                        Object.Destroy(chunks[i].Obj);
                    }
                }
            }

            _spawnedMapObjects.Clear();
            _mapGridDataDic.Clear();
            _activeGrids.Clear();
            _loadingGrids.Clear();
            _invalidGrids.Clear();
            _gridsToRemove.Clear();
            _keepGrids.Clear();
            _animatingChunksCache.Clear();
        }

        // ===================================================================================
        // [핵심] 그리드 스트리밍 로직 (Hysteresis & Background Loop)
        // ===================================================================================

        private async Awaitable StartGridStreamingLoopAsync()
        {
            const float GRID_SIZE = 64f;
            const float Y_RADIUS  = 64f;

            // 반복 연산을 피하기 위해 루프 밖에서 미리 제곱값 계산
            float unloadRadSq  = UNLOAD_RADIUS * UNLOAD_RADIUS;
            float preloadRadSq = PRELOAD_RADIUS * PRELOAD_RADIUS;

            // 검사할 그리드 셀 범위: 반경 ÷ 그리드 크기 + 여유 1칸
            // PRELOAD_RADIUS(10) < GRID_SIZE(64)이므로 현재 상수 기준 keepRange=2, yRange=1
            int keepRange = Mathf.CeilToInt(UNLOAD_RADIUS / GRID_SIZE) + 1;
            int yRange    = Mathf.CeilToInt(Y_RADIUS / GRID_SIZE);

            while (true == _isStreamingActive
                   && true == _cameraTransform)
            {
                Vector3 camPos = _cameraTransform.position;
                float camX = camPos.x;
                float camY = camPos.y;
                float camZ = camPos.z;

                // 카메라가 속한 그리드 좌표
                int camGx = Mathf.FloorToInt(camX / GRID_SIZE);
                int camGy = Mathf.FloorToInt(camY / GRID_SIZE);
                int camGz = Mathf.FloorToInt(camZ / GRID_SIZE);

                _keepGrids.Clear();

                // 1. 이웃 그리드 셀을 직접 열거하여 Preload·Keep 판정
                //    월드 좌표 샘플링 방식은 그리드 경계 근처에서 탐지 누락이 발생하므로,
                //    그리드 AABB ↔ 카메라 수평 최단 거리로 판정한다.
                for (int dy = -yRange; dy <= yRange; dy++)
                {
                    for (int dx = -keepRange; dx <= keepRange; dx++)
                    {
                        for (int dz = -keepRange; dz <= keepRange; dz++)
                        {
                            int gx = camGx + dx;
                            int gy = camGy + dy;
                            int gz = camGz + dz;

                            // 그리드 AABB에서 카메라까지의 수평 최단 거리 계산
                            float nearX  = Mathf.Clamp(camX, gx * GRID_SIZE, (gx + 1) * GRID_SIZE);
                            float nearZ  = Mathf.Clamp(camZ, gz * GRID_SIZE, (gz + 1) * GRID_SIZE);
                            float ddx    = camX - nearX;
                            float ddz    = camZ - nearZ;
                            float distSq = ddx * ddx + ddz * ddz;

                            // Unload 반경 밖 → 완전 무시
                            if (distSq > unloadRadSq) continue;

                            // 그리드 키 계산 (MapCoordUtil.ComputeGridKey와 동일한 패킹)
                            byte bX = (byte)(sbyte)gx;
                            byte bY = (byte)(sbyte)gy;
                            byte bZ = (byte)(sbyte)gz;
                            int targetGridKey = (bX << 16) | (bY << 8) | bZ;

                            // ★ 블랙리스트 필터링
                            if (_invalidGrids.Contains(targetGridKey)) continue;

                            // Unload 반경 내 → Keep (히스테리시스 구간 포함)
                            _keepGrids.Add(targetGridKey);

                            // Preload 반경 밖 → 유지는 하되 신규 로드는 안 함
                            if (distSq > preloadRadSq) continue;

                            if (_activeGrids.Contains(targetGridKey)
                                || !_loadingGrids.Add(targetGridKey))
                            {
                                continue;
                            }

                            _ = LoadGridDataAsync(targetGridKey); // Fire and forget
                        }
                    }
                }

                // 2. Unload: Keep 반경을 벗어난 그리드 식별
                _gridsToRemove.Clear();
                foreach (int loadedGridKey in _activeGrids)
                {
                    if (!_keepGrids.Contains(loadedGridKey))
                    {
                        _gridsToRemove.Add(loadedGridKey);
                    }
                }

                // 3. 실제 언로드 처리
                for (int i = 0; i < _gridsToRemove.Count; ++i)
                {
                    UnloadGridData(_gridsToRemove[i]);
                }

                await Awaitable.WaitForSecondsAsync(CHECK_INTERVAL);
            }
        }

        private async Awaitable LoadGridDataAsync(int gridKey)
        {
            try
            {
                // 문자열 보간 캐싱으로 GC Alloc 방지
                if (!_gridKeyAddressCache.TryGetValue(gridKey, out string addressKey))
                {
                    addressKey = $"MapNavi_{gridKey}";
                    _gridKeyAddressCache[gridKey] = addressKey;
                }

                MapGridData gridData = await AssetProvider.ReadBinaryDataAsync<MapGridData>(addressKey);

                if (gridData == null)
                {
                    // ★ 핵심 방어: Addressables에 존재하지 않는 그리드는 블랙리스트에 영구 등록
                    _invalidGrids.Add(gridKey);
                    return;
                }

                _mapGridDataDic[gridKey] = gridData;

                if (gridData.layerMeshAssets != null)
                {
                    _spawnedMapObjects[gridKey] = new List<MapChunkContext>();
                    for (int i = 0; i < gridData.layerMeshAssets.Count; i++)
                    {
                        await CreateMapChunksAsync(gridKey, gridData.layerMeshAssets[i]);
                    }
                }

                _activeGrids.Add(gridKey);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MapManager] Grid {gridKey} 로드 중 오류: {e.Message}");
            }
            finally
            {
                _loadingGrids.Remove(gridKey);
            }
        }

        // ===================================================================================
        // [핵심] 메쉬 생성 및 타임 슬라이싱 (렉 방지)
        // ===================================================================================

        private async Awaitable CreateMapChunksAsync(int gridKey, MapGridLayerData layerData)
        {
            int instantiateCounter = 0;

            for (int i = 0; i < layerData.assets.Count; i++)
            {
                string meshAddress = layerData.assets[i];
                Mesh bakedMesh = await AssetProvider.LoadAssetAsync<Mesh>(meshAddress);
                if (!bakedMesh)
                {
                    continue;                    
                }
                
                string matAddress = GetMaterialAddress(meshAddress);
                Material mat = await AssetProvider.LoadAssetAsync<Material>(matAddress);

                // 오브젝트 조립
                GameObject chunkObj = new GameObject(meshAddress);
                chunkObj.transform.SetParent(_rootTransform);
                chunkObj.transform.position = Vector3.zero;

                MeshFilter filter = chunkObj.AddComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;

                MeshRenderer renderer = chunkObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat ? mat : new Material(Shader.Find("Standard"));

                // MapChunk 엔티티 생성 및 관리 리스트 추가
                MapChunkContext chunk = new MapChunkContext
                {
                    Layer = layerData.layer,
                    Obj = chunkObj,
                    Renderer = renderer,
                    CurrentColor = Color.white
                };

                _spawnedMapObjects[gridKey].Add(chunk);

                // 💡 Time-Slicing: 3개 생성마다 한 프레임 쉬어감으로써 메인 스레드 점유율 제어
                instantiateCounter++;
                if (instantiateCounter % 3 == 0)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
        }

        private void UnloadGridData(int gridKey)
        {
            if (_spawnedMapObjects.TryGetValue(gridKey, out List<MapChunkContext> chunks))
            {
                for (int i = 0; i < chunks.Count; ++i)
                {
                    if (chunks[i].Obj)
                    {
                        Object.Destroy(chunks[i].Obj);
                    }
                }
                _spawnedMapObjects.Remove(gridKey);
            }

            _mapGridDataDic.Remove(gridKey);
            _activeGrids.Remove(gridKey);
        }

        // ===================================================================================
        // [핵심] 레이어 시각적 제어 (Async Fade)
        // ===================================================================================

        public async Awaitable UpdateLayerVisibilityAsync(int currentLayer, bool hideInsteadOfDim = false, float duration = 1.0f)
        {
            _layerTransitionToken++;
            int currentToken = _layerTransitionToken;

            Color normalColor = Color.white;
            Color dimColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            Color hideColor = new Color(0f, 0f, 0f, 0f);

            // 매 프레임 Dictionary를 순회하지 않도록 애니메이션 대상만 1차원 리스트로 캐싱
            _animatingChunksCache.Clear();

            foreach (var kvp in _spawnedMapObjects)
            {
                List<MapChunkContext> gridChunks = kvp.Value;
                for (int i = 0; i < gridChunks.Count; i++)
                {
                    MapChunkContext chunk = gridChunks[i];
                    chunk.StartColor = chunk.CurrentColor;

                    if (chunk.Layer == currentLayer)
                    {
                        chunk.TargetColor = normalColor;
                        if (!chunk.Renderer.enabled) chunk.Renderer.enabled = true;
                    }
                    else
                    {
                        chunk.TargetColor = hideInsteadOfDim ? hideColor : dimColor;
                        if (!chunk.Renderer.enabled && !hideInsteadOfDim) chunk.Renderer.enabled = true;
                    }

                    _animatingChunksCache.Add(chunk);
                }
            }

            // 보간 루프 (메인 스레드 부하 최소화)
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_layerTransitionToken != currentToken) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 평탄화된 1차원 리스트만 순회
                for (int i = 0; i < _animatingChunksCache.Count; i++)
                {
                    MapChunkContext chunk = _animatingChunksCache[i];
                    chunk.CurrentColor = Color.Lerp(chunk.StartColor, chunk.TargetColor, t);

                    chunk.Renderer.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                    chunk.Renderer.SetPropertyBlock(_propBlock);
                }

                await Awaitable.NextFrameAsync();
            }

            // 최종 상태 확정
            if (_layerTransitionToken != currentToken) return;

            for (int i = 0; i < _animatingChunksCache.Count; i++)
            {
                MapChunkContext chunk = _animatingChunksCache[i];
                chunk.CurrentColor = chunk.TargetColor;

                chunk.Renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                chunk.Renderer.SetPropertyBlock(_propBlock);

                if (chunk.Layer != currentLayer && hideInsteadOfDim)
                {
                    chunk.Renderer.enabled = false;
                }
            }

            // 참조 해제 (메모리 릭 방지)
            _animatingChunksCache.Clear();
        }

        // ===================================================================================
        // 데이터 접근 (Field 레이어 → IMapQueryService 경유)
        // ===================================================================================

        /// <summary>
        /// 월드 좌표를 받아 해당 위치의 MapTileData를 반환합니다.
        /// 그리드가 로드되지 않았거나 타일이 없으면 false를 반환합니다.
        /// </summary>
        public bool TryGetTileData(in Unity.Mathematics.float3 worldPos, out MapTileData tileData)
        {
            MapCoordUtil.ComputeKey(worldPos, out int gKey, out int tKey);
            if (_mapGridDataDic.TryGetValue(gKey, out MapGridData gridData))
            {
                return gridData.TryGetTileData(tKey, out tileData);
            }
            tileData = default;
            return false;
        }

        // ===================================================================================
        // 유틸리티
        // ===================================================================================

        private string GetMaterialAddress(string meshName)
        {
            // 캐싱을 통해 무거운 Substring 연산과 문자열 보간을 최초 1회로 제한
            if (_materialAddressCache.TryGetValue(meshName, out string cachedMatAddress))
            {
                return cachedMatAddress;
            }

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
    }
}