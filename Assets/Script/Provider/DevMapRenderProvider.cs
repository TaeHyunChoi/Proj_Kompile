namespace Kompile.Map.Provider
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// [Framework] Runtime Provider: 인게임에서 구워진 맵 메쉬와 머티리얼을 로드하고 배치합니다.
    /// </summary>
    public class DevMapRenderProvider
    {
        // 부모가 될 최상위 컨테이너
        private Transform _mapRoot;

        public DevMapRenderProvider(Transform mapRoot)
        {
            _mapRoot = mapRoot;
        }

        /// <summary>
        /// MapGridData에 기록된 에셋 이름 배열을 순회하며 비동기로 화면에 생성합니다.
        /// </summary>
        public async Awaitable LoadMapChunkAsync(string meshAssetName)
        {
            // 1. 문자열 파싱 (예: "MapRender_0_G0_L0_Town_Field_0")
            string[] parts = meshAssetName.Split('_');
            if (parts.Length < 7)
            {
                Debug.LogError($"[Framework] 잘못된 맵 에셋 이름 형식: {meshAssetName}");
                return;
            }

            // 인덱스 4는 TopAtlas 이름, 5는 SideAtlas 이름
            string topAtlasName = parts[4];
            string sideAtlasName = parts[5];
            string matName = $"Mat_{topAtlasName}_{sideAtlasName}";

            // 2. 두 핸들을 동시에 시작한 뒤 순서대로 await (병렬 로드 유지)
            var meshHandle = Addressables.LoadAssetAsync<Mesh>(meshAssetName);
            var matHandle  = Addressables.LoadAssetAsync<Material>(matName);

            Mesh     mesh = await meshHandle.Task;
            Material mat  = await matHandle.Task;

            if (meshHandle.Status == AsyncOperationStatus.Succeeded
             && matHandle.Status  == AsyncOperationStatus.Succeeded)
            {
                // 3. 인게임 오브젝트 직조
                GameObject chunkObj = new GameObject(meshAssetName);
                chunkObj.transform.SetParent(_mapRoot);

                MeshFilter filter = chunkObj.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                MeshRenderer renderer = chunkObj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;
            }
            else
            {
                Debug.LogError($"[Framework] 맵 데이터 로드 실패: {meshAssetName} 또는 {matName}");
            }
        }
    }
}