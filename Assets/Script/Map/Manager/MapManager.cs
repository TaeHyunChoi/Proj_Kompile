namespace Script.Map.Manager
{
    using Script.Map.Utility;
    using Script.Map.Data;
    using Script.Asset.Provider;
    using UnityEngine;
    using System.Collections.Generic;

    public class MapManager
    {
        private class MapChunk
        {
            public int Layer;
            public GameObject Obj;
            public MeshRenderer Renderer;

            public Color StartColor;
            public Color TargetColor;
            public Color CurrentColor = Color.white; // 초기값
        }

        private readonly Dictionary<int, MapGridData> _mapGridDataDic;
        private readonly Dictionary<int, List<MapChunk>> _spawnedMapObjects; // GameObject에서 MapChunk로 변경
        private readonly Transform _rootTransform;

        // 2. 머티리얼 속성 조작을 위한 프로퍼티 블록 (할당 최적화를 위해 캐싱)
        private readonly MaterialPropertyBlock _propBlock;
        private static readonly int ColorPropID = Shader.PropertyToID("_Color");

        // 중복 실행 방지 및 최신 호출을 추적하기 위한 수동 플래그
        private int _layerTransitionToken = 0;

        public MapManager(Transform root)
        {
            _mapGridDataDic = new Dictionary<int, MapGridData>();
            _spawnedMapObjects = new Dictionary<int, List<MapChunk>>();
            _rootTransform = root;

            _propBlock = new MaterialPropertyBlock();
        }

        public async Awaitable InitializeAsync(Vector3 pos)
        {
            AssetRepoProvider.Initialize();
            await Init(pos);
        }

        private async Awaitable Init(Vector3 pos)
        {
            int gridKey = MapCoordUtil.ComputeGridKey(new Unity.Mathematics.float3(pos.x, pos.y, pos.z));
            if (_mapGridDataDic.ContainsKey(gridKey))
            {
                return;
            }

            MapGridData gridData = await AssetRepoProvider.ReadBinaryDataAsync<MapGridData>($"MapNavi_{gridKey}");
            if (null == gridData)
            {
                Debug.LogError($"[MapManager] {gridKey} 데이터를 로드하지 못했습니다.");
                return;
            }

            _mapGridDataDic[gridKey] = gridData;

            if (null != gridData.layerMeshAssets)
            {
                _spawnedMapObjects[gridKey] = new List<MapChunk>();
                foreach (var layerData in gridData.layerMeshAssets)
                {
                    await CreateMapChunksAsync(gridKey, layerData);
                }
            }
        }

        private async Awaitable CreateMapChunksAsync(int gridKey, MapGridLayerData layerData)
        {
            foreach (var meshAddress in layerData.assets)
            {
                Mesh bakedMesh = await AssetRepoProvider.LoadAssetAsync<Mesh>(meshAddress);

                if (bakedMesh == null)
                {
                    Debug.LogWarning($"[MapManager] fail to load mesh. address? {meshAddress}");
                    continue;
                }

                string matAddress = GetMaterialAddress(meshAddress);
                Material mat = await AssetRepoProvider.LoadAssetAsync<Material>(matAddress);

                GameObject chunk = new GameObject(meshAddress);
                chunk.transform.SetParent(_rootTransform);
                chunk.transform.position = Vector3.zero;

                var filter = chunk.AddComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;

                var renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat != null ? mat : new Material(Shader.Find("Standard"));

                // 생성된 오브젝트를 MapChunk로 래핑하여 리스트에 보관
                _spawnedMapObjects[gridKey].Add(new MapChunk
                {
                    Layer = layerData.layer,
                    Obj = chunk,
                    Renderer = renderer
                });
            }
        }

        public async Awaitable UpdateLayerVisibilityAsync(int currentLayer, bool hideInsteadOfDim = false, float duration = 1.0f)
        {
            // 함수가 호출될 때마다 토큰을 갱신하여 이전의 보간 루프를 무효화합니다.
            int currentToken = ++_layerTransitionToken;

            Color normalColor = Color.white;
            Color dimColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            Color hideColor = new Color(0f, 0f, 0f, 0f); // 투명/검정

            // 1. 모든 청크의 목표 색상(Target) 설정 및 렌더러 활성화
            foreach (var gridChunks in _spawnedMapObjects.Values)
            {
                foreach (MapChunk chunk in gridChunks)
                {
                    chunk.StartColor = chunk.CurrentColor;

                    if (chunk.Layer == currentLayer)
                    {
                        chunk.TargetColor = normalColor;
                        if (!chunk.Renderer.enabled) chunk.Renderer.enabled = true; // 보이게 전환되므로 즉시 켬
                    }
                    else
                    {
                        if (hideInsteadOfDim)
                        {
                            chunk.TargetColor = hideColor;
                        }
                        else
                        {
                            chunk.TargetColor = dimColor;
                            if (!chunk.Renderer.enabled) chunk.Renderer.enabled = true;
                        }
                    }
                }
            }

            // 2. Awaitable을 활용한 색상 보간(Lerp) 루프
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 도중에 플레이어가 다른 층으로 이동해 새 토큰이 발급되었다면 즉시 루프 종료
                if (_layerTransitionToken != currentToken)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                foreach (var gridChunks in _spawnedMapObjects.Values)
                {
                    foreach (MapChunk chunk in gridChunks)
                    {
                        chunk.CurrentColor = Color.Lerp(chunk.StartColor, chunk.TargetColor, t);

                        chunk.Renderer.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                        chunk.Renderer.SetPropertyBlock(_propBlock);
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            // 3. 루프 종료 후 최종 상태 확정
            if (_layerTransitionToken != currentToken)
            {
                return;
            }

            foreach (var gridChunks in _spawnedMapObjects.Values)
            {
                foreach (MapChunk chunk in gridChunks)
                {
                    chunk.CurrentColor = chunk.TargetColor;

                    chunk.Renderer.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(ColorPropID, chunk.CurrentColor);
                    chunk.Renderer.SetPropertyBlock(_propBlock);

                    // 숨기기 모드일 경우, 페이드 아웃(색상 변경)이 완전히 끝난 후 렌더러를 끕니다.
                    if (chunk.Layer != currentLayer && hideInsteadOfDim)
                    {
                        chunk.Renderer.enabled = false;
                    }
                }
            }
        }

        private string GetMaterialAddress(string meshName)
        {
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