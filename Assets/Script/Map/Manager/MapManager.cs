namespace Script.Map.Manager
{
    using Script.Map.Utility;
    using Script.Map.Data;
    using Script.Asset.Provider;
    using UnityEngine;
    using System.Collections.Generic;

    public class MapManager : MonoBehaviour
    {
        private Dictionary<int, MapGridData> _mapGridDataDic = new Dictionary<int, MapGridData>();
        private Dictionary<int, List<GameObject>> _spawnedMapObjects = new Dictionary<int, List<GameObject>>();

        // [수정 1] async void로 변경하여 비동기 에러가 유니티 콘솔에 찍히도록 보장합니다.
        private async void Start()
        {
            // [수정 2] Provider의 딕셔너리들을 사용할 수 있도록 초기화합니다.
            // (만약 최상위 GameManager 등에서 이미 호출하고 있다면 이 줄은 생략해도 됩니다.)
            AssetRepoProvider.Initialize();

            // [수정 3] await를 붙여서 작업이 끝날 때까지 대기하고, 예외 발생 시 캐치합니다.
            await Init(new Vector3(1.5f, 0f, 1f));
        }

        public async Awaitable Init(Vector3 pos)
        {
            var gridKey = MapCoordUtil.ComputeGridKey(new Unity.Mathematics.float3(pos.x, pos.y, pos.z));

            if (_mapGridDataDic.ContainsKey(gridKey))
                return;

            Debug.Log($"[MapManager] 그리드 {gridKey} 로드 시작...");

            MapGridData gridData = await AssetRepoProvider.ReadBinaryDataAsync<MapGridData>($"MapNavi_{gridKey}");
            if (gridData == null)
            {
                Debug.LogError($"[MapManager] {gridKey} 데이터를 로드하지 못했습니다.");
                return;
            }

            _mapGridDataDic[gridKey] = gridData;

            if (gridData.layerMeshAssets != null)
            {
                _spawnedMapObjects[gridKey] = new List<GameObject>();

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
                    // [수정 4] 만약 정말로 로드에 실패했다면 이유를 알 수 있도록 로그를 추가합니다.
                    Debug.LogWarning($"[MapManager] 메쉬 로드 실패! 주소를 확인하세요: {meshAddress}");
                    continue;
                }

                string matAddress = GetMaterialAddress(meshAddress);
                Material mat = await AssetRepoProvider.LoadAssetAsync<Material>(matAddress);

                GameObject chunk = new GameObject(meshAddress);
                chunk.transform.SetParent(this.transform);
                chunk.transform.position = Vector3.zero;

                var filter = chunk.AddComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;

                var renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat != null ? mat : new Material(Shader.Find("Standard"));

                _spawnedMapObjects[gridKey].Add(chunk);
            }
        }

        private string GetMaterialAddress(string meshName)
        {
            // 베이킹 포맷: MapRender_{Scene}_G{Grid}_L{Layer}_{TopAtlas}_{SideAtlas}_{PartIdx}
            // 예시: MapRender_0_G123_L0_merged-test_2_merged-test_2_0

            int lastUnderScore = meshName.LastIndexOf('_');
            if (lastUnderScore == -1) return "Mat_Default";

            // 앞에서부터 4번째 '_'의 위치를 찾습니다.
            int prefixEndIndex = -1;
            int underscoreCount = 0;

            for (int i = 0; i < meshName.Length; i++)
            {
                if (meshName[i] == '_')
                {
                    underscoreCount++;
                    if (underscoreCount == 4)
                    {
                        prefixEndIndex = i;
                        break;
                    }
                }
            }

            // 4번째 '_'와 마지막 '_' 사이의 문자열을 잘라냅니다.
            if (prefixEndIndex != -1 && lastUnderScore > prefixEndIndex)
            {
                // 결과: "merged-test_2_merged-test_2"
                string atlases = meshName.Substring(prefixEndIndex + 1, lastUnderScore - prefixEndIndex - 1);
                return $"Mat_{atlases}";
            }

            return "Mat_Default";
        }
    }
}