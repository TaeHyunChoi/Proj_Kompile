#if UNITY_EDITOR
namespace MapSampling
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using Script.Util;
    using Script.Data;


    /// <summary> 
    /// reference link: https://www.youtube.com/watch?v=K-zw3QFaTqg
    /// 어드레서블 에셋 라벨 로드 : https://chatgpt.com/share/67b4abb9-aea8-8008-8849-01ed2e70af15
    /// </summary>
    public class MapTileSampling : MonoBehaviour
    {
        private readonly string assetGroupName = "MapRender";

        [SerializeField] private Transform instanceTransform;
        private ConcurrentDictionary<int, RawMapGridData> map;

        public async void Save()
        {
            // set data
            EditMapData[] tiles = instanceTransform.GetComponentsInChildren<EditMapData>();
            if (0 == tiles.Length)
            {
                Debug.LogWarning("NavTileMesh.Length = 0;");
                return;
            }

            // async : nav data
            Task taskSaveNavData = SaveMapNavDataAsync(tiles);

            // sync : render data (unity api 사용하므로 async 불가)
            //StartCoroutine(IESaveRender(tiles));
            StartCoroutine(IESaveMesh(tiles));

            await Task.WhenAll(taskSaveNavData);
            taskSaveNavData.Dispose();

            AssetDatabase.Refresh();
            Debug.Log("모든 Temp 오브젝트의 Init 호출이 병렬로 완료되었습니다.");
        }
        public async Task SaveMapNavDataAsync(EditMapData[] tiles)
        {
            map = new ConcurrentDictionary<int, RawMapGridData>();
            int length = tiles.Length;
            int i, t;

            // bake + dispose
            Task[] initTasks = new Task[length];
            for (i = 0; i < length; i++)
            {
                t = i;
                initTasks[t] = tiles[t].BakeMesh(map);
            }
            await Task.WhenAll(initTasks);
            for (i = 0; i < length; i++)
            {
                t = i;
                initTasks[t].Dispose();
            }

            // save data
            foreach (var grid in map)
            {
                DataManager.WriteBinaryMappingData<RawMapGridData>(grid.Value, $"MapNavi_{grid.Key}");
            }
        }
        private IEnumerator IESaveMesh(EditMapData[] tiles)
        {
            Dictionary<long, List<CombineInstance>> combined = new Dictionary<long, List<CombineInstance>>();
            Dictionary<long, List<Vector2>> combinedUVs = new Dictionary<long, List<Vector2>>();

            EditMapData tile;
            for (int i = 0; i < tiles.Length; ++i)
            {
                tile = tiles[i];
                long key = tile.GridKey << 32 | tile.Layer;

                // 없으면 새로 넣고
                if (false == combined.ContainsKey(key))
                {
                    combined.Add(key, new List<CombineInstance>());
                    combinedUVs.Add(key, new List<Vector2>());
                }

                CombineInstance combInstance = new CombineInstance();
                combInstance.mesh = Object.Instantiate(tile.MeshFilter.sharedMesh); // 새로운 인스턴스를 생성하여 UV 설정
                combInstance.transform = tile.transform.localToWorldMatrix;
                Vector2[] uvs = GetUVs(combInstance, tile.TextureIndex); // 개별적으로 UV 설정
                combInstance.mesh.uv = uvs;

                combined[key].Add(combInstance);
                combinedUVs[key].AddRange(uvs);
            }

            foreach (var inst in combined)
            {
                long key = inst.Key;
                int gridKey = (int)(key >> 32);
                int layer = (int)(key & 0xFFFF_FFFF);

                Mesh combinedMesh = new Mesh();
                combinedMesh.CombineMeshes(inst.Value.ToArray(), true, true, false); // UV를 유지하도록 CombineMeshes 호출

                // 병합된 메쉬의 UV 배열을 수동으로 설정합니다.
                combinedMesh.uv = combinedUVs[key].ToArray();

                SaveMesh(combinedMesh, gridKey, layer, true, false);
                yield return null;
            }

            EditorUtility.SetDirty(AddressableAssetSettingsDefaultObject.Settings);
        }
        private void SaveMesh(Mesh mesh, int gridKey, int layer, bool makeNewInstance, bool optimizeMesh)
        {

            string labelName = $"MapRender_{gridKey}";
            string assetName = $"MapRender_{gridKey}_{layer}";

            var path = "Assets/Rcs/MapRender/" + $"MapRender_{gridKey}_{layer}" + ".asset";

            // 이미 같은 이름의 에셋이 있는지 확인합니다.
            if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            var meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
            if (optimizeMesh)
            {
                MeshUtility.Optimize(meshToSave);
            }

            AssetDatabase.CreateAsset(meshToSave, path);

            // Addressable Assets에 등록
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(assetGroupName);
            if (null != group)
            {
                // Addressable 에셋 생성
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                entry.SetAddress(assetName);
                entry.labels.Add(labelName);

                EditorUtility.SetDirty(settings);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }
            else
            {
                Debug.LogError("Addressable Asset Group not found.");
                return;
            }

            AssetDatabase.SaveAssets();
        }

        private Vector2[] GetUVs(CombineInstance target, int textureIndex)
        {
            // for test? 이거 맞겠지?
            float spriteSize = 256f;
            int altasWidth   = 2048;
            int altasHeight  = 2048;

            // atlas 내 몇 칸으로 배치되었는지 계산 (좌측 하단 기준)
            int atlasCols = (int)(altasWidth / spriteSize);
            int col = textureIndex % atlasCols;
            int row = textureIndex / atlasCols;

            // 해당 스프라이트의 uv 크기 (atlas 내 비율)
            float uvWidth = (float)spriteSize / altasWidth;
            float uvHeight = (float)spriteSize / altasHeight;

            // 해당 스프라이트의 atlas 내 시작 UV (좌측 하단 좌표) // 이거 좌측 상단 기준이어야 하는거 아닌가?
            float uvX = col * uvWidth;
            float uvY = 1 - row * uvHeight;

            Vector3[] vertices = target.mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                // vertices[i]의 x, y가 이미 0~1 범위라고 가정
                float normalizedX = vertices[i].x;
                float normalizedY = vertices[i].y;

                // 변환 공식: sprite 영역의 시작 UV + (로컬 좌표 * sprite 영역의 UV 크기)
                uvs[i] = new Vector2(uvX + normalizedX * uvWidth,
                                     uvY + normalizedY * uvHeight);
            }

            return uvs;
        }




        public async void Load()
        {
            // 테스트용으로 걸었던 것인디...
            RawMapGridData data = await DataManager.ReadBinaryMappingDataAsync<RawMapGridData>(0);

            // scale ,x[sign,small_buffer,6], y[sign,small_buffer,4], z[sign,small_buffer,6]
            const byte shiftTileLayer   = 23;
            const byte shiftIsHalfScale = 22;
            const byte shiftTileX       = 14;
            const byte shiftTileY       = 8;
            const byte shiftTileZ       = 0;

            foreach (var key in data.rawMapNavData.Keys)
            {
                var layer   = (key >> shiftTileLayer) & 1;
                var scale   = (key >> shiftIsHalfScale) & 1;

                var x = (key >> shiftTileX) & 0xFF;
                var y = (key >> shiftTileY) & 0x0F;
                var z = (key >> shiftTileZ) & 0xFF;

                Debug.Log($"[layer:{layer}][scale:{scale}][{x},{y},{z}]  [navi:{data.rawMapNavData[key].naviMask}], [info:{data.rawMapNavData[key].infoMask}]");

            }
        }
    }
}
#endif 