namespace Script.Map.Manager
{
    using Script.Map.Utility;
    using Script.Map.Data;
    using Script.Asset.Provider; // 나으리가 공유해주신 Provider
    using UnityEngine;
    using System.Collections.Generic;

    public class MapManager : MonoBehaviour
    {
        // 로드된 그리드 데이터를 관리 (Instance-Centric)
        private Dictionary<int, MapGridData> _mapGridDataDic = new Dictionary<int, MapGridData>();
        
        // 생성된 맵 오브젝트들을 관리 (나중에 해제할 때 사용)
        private Dictionary<int, List<GameObject>> _spawnedMapObjects = new Dictionary<int, List<GameObject>>();

        public async Awaitable Init(Vector3 pos)
        {
            // 1. 그리드 키 계산
            var gridKey = MapCoordUtil.ComputeGridKey(pos);
            
            if (_mapGridDataDic.ContainsKey(gridKey)) 
                return;

            Debug.Log($"[MapManager] 그리드 {gridKey} 로드 시작...");

            // 2. 바이너리 데이터 로드 (Addressables 명칭: MapNavi_{gridKey})
            // AssetRepoProvider의 ReadBinaryDataAsync를 직접 사용합니다.
            MapGridData gridData = await AssetRepoProvider.ReadBinaryDataAsync<MapGridData>($"MapNavi_{gridKey}");
            if (gridData == null)
            {
                Debug.LogError($"[MapManager] {gridKey} 데이터를 로드하지 못했습니다.");
                return;
            }

            _mapGridDataDic[gridKey] = gridData;

            // 3. 베이킹된 메쉬 자원 로드 및 생성
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
                // A. 메쉬 로드 (AssetRepoProvider 사용)
                // AssetKey는 암시적 변환이 되므로 string으로 전달 가능
                Mesh bakedMesh = await AssetRepoProvider.LoadAssetAsync<Mesh>(meshAddress);
                if (bakedMesh == null) continue;

                // B. 머티리얼 주소 추론 (Bake 규칙에 따라)
                string matAddress = GetMaterialAddress(meshAddress);
                Material mat = await AssetRepoProvider.LoadAssetAsync<Material>(matAddress);

                // C. 오브젝트 조립
                GameObject chunk = new GameObject(meshAddress);
                chunk.transform.SetParent(this.transform);
                
                // [나으리 확인!] 전체 씬 굽기이므로 좌표는 0,0,0으로 고정합니다.
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
            // 나으리의 Bake 규칙: MapRender_{Scene}_G{Grid}_L{Layer}_{TopAtlas}_{SideAtlas}_{Part}
            // 마지막 PartIndex를 떼고 아틀라스 이름들을 추출합니다.
            string[] parts = meshName.Split('_');
            if (parts.Length >= 3)
            {
                // 뒤에서 3번째: TopAtlas, 뒤에서 2번째: SideAtlas
                string top = parts[parts.Length - 3];
                string side = parts[parts.Length - 2];
                return $"Mat_{top}_{side}";
            }
            return "Mat_Default";
        }
    }
}