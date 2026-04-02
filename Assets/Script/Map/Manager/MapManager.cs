namespace Script.Map.Manager
{
    using Script.Map.Utility;
    using Script.Map.Data;
    using Script.Map.Entity;
    using Script.Asset.Provider;
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// [Framework] Manager 계층
    /// 인게임 맵 그리드의 동적 스트리밍, 레이어 시각적 제어, 실시간 인스턴스 관리를 전담합니다.
    /// </summary>
    public class MapManager
    {
        // --- Manager State (Instance-Centric) ---
        private readonly Dictionary<int, MapGridData> _mapGridDataDic;
        private readonly Dictionary<int, List<MapChunkContext>> _spawnedMapObjects;
        private readonly Transform _rootTransform;

        // --- Streaming State ---
        private readonly HashSet<int> _activeGrids;      // 로드 완료된 그리드 키
        private readonly HashSet<int> _loadingGrids;     // 현재 로딩 프로세스 중인 그리드 키
        private readonly List<int> _gridsToRemove;    // 언로드 계산용 임시 리스트 (GC 방지)
        private readonly HashSet<int> _keepGrids;        // 언로드 방지용 임시 셋 (GC 방지)

        // --- Rendering & Visuals ---
        private readonly MaterialPropertyBlock _propBlock;
        private static readonly int ColorPropID = Shader.PropertyToID("_Color");
        private int _layerTransitionToken = 0; // 레이어 전환 애니메이션 중첩 방지 토큰

        // --- Camera & Streaming Config ---
        private Transform _cameraTransform;
        private bool _isStreamingActive = false;

        private const float PRELOAD_RADIUS = 15f;  // 로드 시작 반경 (Far 10 + 여유 5)
        private const float UNLOAD_RADIUS = 25f;   // 언로드 시작 반경 (Preload + 10)
        private const float CHECK_INTERVAL = 0.5f; // 스트리밍 검사 주기 (초)

        public MapManager(Transform root)
        {
            _mapGridDataDic = new Dictionary<int, MapGridData>();
            _spawnedMapObjects = new Dictionary<int, List<MapChunkContext>>();
            _activeGrids = new HashSet<int>();
            _loadingGrids = new HashSet<int>();
            _gridsToRemove = new List<int>(16);
            _keepGrids = new HashSet<int>();

            _rootTransform = root;
            _propBlock = new MaterialPropertyBlock();
        }

        // ===================================================================================
        // 시스템 제어 인터페이스
        // ===================================================================================

        public async Awaitable InitializeAsync(Transform cameraTransform)
        {
            AssetRepoProvider.Initialize();
            _cameraTransform = cameraTransform;
            _isStreamingActive = true;

            // 배경 스트리밍 루프 시작
            _ = StartGridStreamingLoopAsync();
        }

        public void StopStreaming()
        {
            _isStreamingActive = false;
        }

        // ===================================================================================
        // [핵심] 그리드 스트리밍 로직 (Hysteresis & Background Loop)
        // ===================================================================================

        private async Awaitable StartGridStreamingLoopAsync()
        {
            while (_isStreamingActive && _cameraTransform != null)
            {
                Vector3 camPos = _cameraTransform.position;
                _keepGrids.Clear();

                // 1. Preload & Keep 영역 동시 계산 (판정 기준 통일)
                float step = 5f; // 그리드 크기(Grid Size)보다 작거나 같아야 누락이 없습니다.
                for (float x = -UNLOAD_RADIUS; x <= UNLOAD_RADIUS; x += step)
                {
                    for (float z = -UNLOAD_RADIUS; z <= UNLOAD_RADIUS; z += step)
                    {
                        float distSq = x * x + z * z;
                        // UNLOAD 반경 밖은 무시
                        if (distSq > UNLOAD_RADIUS * UNLOAD_RADIUS) continue;

                        Vector3 checkPos = camPos + new Vector3(x, 0, z);
                        int targetGridKey = MapCoordUtil.ComputeGridKey(new Unity.Mathematics.float3(checkPos.x, 0, checkPos.z));

                        // UNLOAD 반경 안에 걸친 그리드는 파괴 방지 목록에 등록
                        _keepGrids.Add(targetGridKey);

                        // PRELOAD 반경 안에 들어오면 로드 시도
                        if (distSq <= PRELOAD_RADIUS * PRELOAD_RADIUS)
                        {
                            if (!_activeGrids.Contains(targetGridKey) && !_loadingGrids.Contains(targetGridKey))
                            {
                                _loadingGrids.Add(targetGridKey);
                                _ = LoadGridDataAsync(targetGridKey); // Fire and forget
                            }
                        }
                    }
                }

                // 2. Unload: Keep 반경을 벗어난 그리드 식별 (Pivot 기준 거리 계산 제거)
                _gridsToRemove.Clear();
                foreach (int loadedGridKey in _activeGrids)
                {
                    // 파괴 방지 목록에 없는 그리드만 언로드 대상으로 선정
                    if (!_keepGrids.Contains(loadedGridKey))
                    {
                        _gridsToRemove.Add(loadedGridKey);
                    }
                }

                // 3. 실제 언로드 처리
                for (int i = 0; i < _gridsToRemove.Count; i++)
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
                MapGridData gridData = await AssetRepoProvider.ReadBinaryDataAsync<MapGridData>($"MapNavi_{gridKey}");
                if (gridData == null) return;

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
                Mesh bakedMesh = await AssetRepoProvider.LoadAssetAsync<Mesh>(meshAddress);
                if (bakedMesh == null) continue;

                string matAddress = GetMaterialAddress(meshAddress);
                Material mat = await AssetRepoProvider.LoadAssetAsync<Material>(matAddress);

                // 오브젝트 조립
                GameObject chunkObj = new GameObject(meshAddress);
                chunkObj.transform.SetParent(_rootTransform);
                chunkObj.transform.position = Vector3.zero;

                var filter = chunkObj.AddComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;

                var renderer = chunkObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat != null ? mat : new Material(Shader.Find("Standard"));

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
            if (_spawnedMapObjects.TryGetValue(gridKey, out var chunks))
            {
                for (int i = 0; i < chunks.Count; i++)
                {
                    if (chunks[i].Obj != null) Object.Destroy(chunks[i].Obj);
                }
                _spawnedMapObjects.Remove(gridKey);
            }

            _mapGridDataDic.Remove(gridKey);
            _activeGrids.Remove(gridKey);

            // AssetRepoProvider의 정책에 따라 Addressable 에셋 해제 로직 추가 가능
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

            // 목표 설정
            foreach (var gridChunks in _spawnedMapObjects.Values)
            {
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
                }
            }

            // 보간 루프
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_layerTransitionToken != currentToken) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                foreach (var gridChunks in _spawnedMapObjects.Values)
                {
                    for (int i = 0; i < gridChunks.Count; i++)
                    {
                        MapChunkContext chunk = gridChunks[i];
                        chunk.CurrentColor = Color.Lerp(chunk.StartColor, chunk.TargetColor, t);

                        chunk.Renderer.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                        chunk.Renderer.SetPropertyBlock(_propBlock);
                    }
                }
                await Awaitable.NextFrameAsync();
            }

            // 최종 상태 확정
            if (_layerTransitionToken != currentToken) return;

            foreach (var gridChunks in _spawnedMapObjects.Values)
            {
                for (int i = 0; i < gridChunks.Count; i++)
                {
                    MapChunkContext chunk = gridChunks[i];
                    chunk.CurrentColor = chunk.TargetColor;

                    chunk.Renderer.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                    chunk.Renderer.SetPropertyBlock(_propBlock);

                    if (chunk.Layer != currentLayer && hideInsteadOfDim)
                    {
                        chunk.Renderer.enabled = false;
                    }
                }
            }
        }

        // ===================================================================================
        // 유틸리티
        // ===================================================================================

        private string GetMaterialAddress(string meshName)
        {
            // 4번째 '_'와 마지막 '_' 사이의 아틀라스 명칭 추출 (언더바 포함 대응)
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

            if (prefixEndIndex != -1 && lastUnderScore > prefixEndIndex)
            {
                string atlases = meshName.Substring(prefixEndIndex + 1, lastUnderScore - prefixEndIndex - 1);
                return $"Mat_{atlases}";
            }

            return "Mat_Default";
        }
    }
}