#if UNITY_EDITOR
namespace MapSampling
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using Script.Data;
    using Script.Manager;

    public class MapTileSampling : MonoBehaviour
    {
        private const int VERTEX_LIMIT = 65535;
        private readonly string assetGroupName = "MapRender";
        private readonly string MAP_NAVI_DATA_PATH = "Rcs\\Bin\\MapNavRawData";


        [SerializeField] private Transform instanceTransform;
        [SerializeField] int sceneIndex = 0;

        private ConcurrentDictionary<int, MapGridData> map;
        private bool nowLoading = false;

        public async void Save()
        {
            // set data
            EditMapData[] tiles = instanceTransform.GetComponentsInChildren<EditMapData>();
            if (0 == tiles.Length)
            {
                Debug.LogWarning("NavTileMesh.Length = 0;");
                return;
            }

            // sync : render data (unity api 사용하므로 async 불가)
            //StartCoroutine(IESaveMesh(tiles));
            map = new ConcurrentDictionary<int, MapGridData>();
            IESaveMesh(tiles);

            // async : nav data
            Task taskSaveNavData = SaveMapNavDataAsync(tiles);
            await Task.WhenAll(taskSaveNavData);
            taskSaveNavData.Dispose();

            AssetDatabase.Refresh();
            Debug.Log("모든 Temp 오브젝트의 Init 호출이 병렬로 완료되었습니다.");
        }
        public async Task SaveMapNavDataAsync(EditMapData[] tiles)
        {
            //map = new ConcurrentDictionary<int, MapGridData>();
            int length = tiles.Length;
            int i, t;

            // bake + dispose
            Task[] initTasks = new Task[length];
            for (i = 0; i < length; i++)
            {
                t = i;
                initTasks[t] = tiles[t].BakeMesh(sceneIndex, map);
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
                AssetManager.WriteBinaryFile<MapGridData>(data: grid.Value,
                                                         dataPath: MAP_NAVI_DATA_PATH,
                                                         fileName: $"MapNavi_{grid.Key}",
                                                         addressableGroup: "MapNavi");
            }
        }

        private void IESaveMesh(EditMapData[] tiles)
        {
            Dictionary<long, TempData> tempDataDict = new Dictionary<long, TempData>();

            EditMapData tile;
            for (int i = 0; i < tiles.Length; ++i)
            {
                tile = tiles[i];
                long key = tile.GridKey << 32 | tile.Layer;

                if (!tempDataDict.ContainsKey(key))
                {
                    tempDataDict[key] = new TempData
                    {
                        combineInstances = new List<CombineInstance>(),
                        combinedUVs = new List<Vector2>(),
                        vertexCount = 0,
                        index = 0
                    };
                }

                TempData tempData = tempDataDict[key];

                int currentVertexCount = tempData.vertexCount;
                int tileVertexCount = tile.MeshFilter.sharedMesh.vertexCount;

                if (currentVertexCount + tileVertexCount > VERTEX_LIMIT)
                {
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.CombineMeshes(tempData.combineInstances.ToArray(), true, true);
                    combinedMesh.uv = tempData.combinedUVs.ToArray();

                    SaveMesh(combinedMesh, tile.GridKey, tile.Layer, tempData.index, true, false);

                    tempData.combineInstances.Clear();
                    tempData.combinedUVs.Clear();
                    tempData.vertexCount = 0;
                    tempData.index++;
                }

                CombineInstance combInstance = new CombineInstance()
                {
                    mesh = tile.MeshFilter.sharedMesh,
                    transform = tile.transform.localToWorldMatrix
                };

                Vector2[] uvs = GetUVs(combInstance, tile.TextureIndex);
                tempData.combineInstances.Add(combInstance);
                tempData.combinedUVs.AddRange(uvs);
                tempData.vertexCount += tileVertexCount;

                tempDataDict[key] = tempData;
            }

            foreach (var kvp in tempDataDict)
            {
                TempData tempData = kvp.Value;
                if (tempData.combineInstances.Count > 0)
                {
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.CombineMeshes(tempData.combineInstances.ToArray(), true, true);
                    combinedMesh.uv = tempData.combinedUVs.ToArray();

                    SaveMesh(combinedMesh, (int)(kvp.Key >> 32), (int)(kvp.Key & 0xFFFF_FFFF), tempData.index, true, false);
                }
            }

            EditorUtility.SetDirty(AddressableAssetSettingsDefaultObject.Settings);
        }
        private void SaveMesh(Mesh mesh, int gridKey, int layer, int index, bool makeNewInstance, bool optimizeMesh)
        {
            if (false == map.ContainsKey(gridKey))
            {
                map.TryAdd(gridKey, new MapGridData(gridKey));
            }

            string assetName = $"MapRender_G{gridKey}_L{layer}_{index}";
            map[gridKey].AddAssetFile(assetName);

            var path = "Assets/Rcs/MapRender/" + assetName + ".asset";
            if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            var meshToSave = (true == makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
            if (true == optimizeMesh)
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
            // scale ,x[sign,small_buffer,6], y[sign,small_buffer,4], z[sign,small_buffer,6]
            const byte shiftTileLayer   = 23;
            const byte shiftIsHalfScale = 22;
            const byte shiftTileX       = 14;
            const byte shiftTileY       = 8;
            const byte shiftTileZ       = 0;

            if (true == nowLoading)
            {
                Debug.Log($"Plz Wait");
                return;
            }

            Debug.Log($"Load Map");

            var time = Time.time;
            nowLoading = true;

            MapGridData data = await AssetManager.ReadBinaryFileAsync<MapGridData>($"MapNavi_{0}");
            Debug.Log($"END LOAD ({Time.time - time:F2} sec)");

            string asset_file = string.Empty;
            for (int i = 0; i < data.assetFiles.Count; ++i)
            {
                asset_file += $"{data.assetFiles[i]}, ";
            }
            Debug.Log($"file: {asset_file}");

            foreach (var key in data.MapNavDataDictionary.Keys)
            {
                var layer   = (key >> shiftTileLayer) & 1;
                var scale   = (key >> shiftIsHalfScale) & 1;

                var x = (key >> shiftTileX) & 0xFF;
                var y = (key >> shiftTileY) & 0x0F;
                var z = (key >> shiftTileZ) & 0xFF;

                Debug.Log($"[layer:{layer}][scale:{scale}][{x},{y},{z}]  [navi:{data.MapNavDataDictionary[key].naviMask}], [info:{data.MapNavDataDictionary[key].infoMask}]");
            }

            nowLoading = false;
        }

        private class TempData
        {
            public List<CombineInstance> combineInstances;
            public List<Vector2> combinedUVs;
            public int vertexCount;
            public int index;
        }
    }
}
#endif 