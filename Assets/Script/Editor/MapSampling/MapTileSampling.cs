#if UNITY_EDITOR
namespace MapSampling
{
    using Script.Data;
    using Script.Manager;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;
    using UnityEngine.Assertions;
    using static EditMapUtil;

    public partial class MapTileSampling : MonoBehaviour
    {
        private const int VERTEX_LIMIT              = 65535;
        private readonly string assetGroupName      = "MapRender";
        private readonly string MAP_NAVI_DATA_PATH  = "Rcs\\Bin\\MapNavRawData";
        private readonly float2[] dir = new float2[]
        {
            new float2( 0, -1),
            new float2( 1,  0),
            new float2( 1, -1),
            new float2( 0,  1),
            new float2( 1,  1),
            new float2(-1,  0),
            new float2(-1,  1),
            new float2(-1, -1)
        };
        private readonly float[] ny = new float[] { 0, 1, -1 };

        [SerializeField] private Transform instanceTransform;
        [SerializeField] int sceneIndex = 0;

        private bool nowLoading = false;

        public void Save()
        {
            Debug.Log($"--- Start to save ---");

            // set data
            EditMapData[] tiles = instanceTransform.GetComponentsInChildren<EditMapData>();
#if UNITY_EDITOR
            Assert.IsTrue(0 != tiles.Length, $"NavTileMesh.Length = {tiles.Length};");
#endif

            // JobSystem -> EditMapData 일괄 생성
            int length                   = tiles.Length;
            var native_array_scene_index = new NativeArray<int>(length, Allocator.TempJob);
            var native_array_nav_layer   = new NativeArray<int>(length, Allocator.TempJob);
            var native_array_position    = new NativeArray<float3>(length, Allocator.TempJob);
            var native_array_rotateY     = new NativeArray<float>(length, Allocator.TempJob);
            var native_array_heights     = new NativeArray<ulong>(length, Allocator.TempJob);
            var native_array_result      = new NativeArray<EditMapTileData>(tiles.Length, Allocator.TempJob);

            EditMapData tileData;
            for (int i = 0; i < tiles.Length; i++)
            {
                tileData = tiles[i];

                native_array_scene_index[i] = sceneIndex;
                native_array_nav_layer[i]   = tileData.NaviLayer;
                native_array_position[i]    = new float3(tileData.transform.position.x, tileData.transform.position.y, tileData.transform.position.z);
                native_array_rotateY[i]     = tileData.transform.eulerAngles.y;
                native_array_heights[i]     = tileData.HeightMask;
            }

            EditMapTileJob job = new EditMapTileJob
            {
                SceneIndex = native_array_scene_index,
                NavLayer   = native_array_nav_layer,
                Position   = native_array_position,
                RotY       = native_array_rotateY,
                Height     = native_array_heights,
                Data       = native_array_result
            };
            JobHandle jobHandle = job.Schedule(tiles.Length, 64);
            jobHandle.Complete();

            //while (false == jobHandle.IsCompleted)
            //{
            //    yield return null;
            //}

            // Map 등록
            var map = new ConcurrentDictionary<int, EditMapGridData>();
            int gridKey, tileKey;
            long naviMask;
            for (int i = 0; i < native_array_result.Length; ++i)
            {
                gridKey = native_array_result[i].GridKey;
                tileKey = native_array_result[i].TileKey;
                naviMask = native_array_result[i].NavMask;

                if (false == map.ContainsKey(gridKey))
                {
                    map.TryAdd(gridKey, new EditMapGridData(gridKey));
                }

                EditMapTileData tile_data = new EditMapTileData()
                {
                    GridKey = gridKey,
                    TileKey = tileKey,
                    NavMask = naviMask
                };
                map[gridKey].TryAdd(tileKey, tile_data);
            }

            float3 start_tile_pivot = native_array_result[0].GetTilePivot();
            native_array_scene_index.Dispose();
            native_array_nav_layer.Dispose();
            native_array_position.Dispose();
            native_array_rotateY.Dispose();
            native_array_heights.Dispose();
            native_array_result.Dispose();

            // DFS 알고리즘 -> EditMaplinkMask 생성
            LinkTiles(map, start_tile_pivot);
            foreach (var m in map.Values)
            {
                foreach (var t in m.Data.Values)
                {
                    Debug.Log($"[{t.GetTilePivot()}] link = {System.Convert.ToString(t.LinkMask, 2)}");
                }
            }
            //yield return StartCoroutine(LinkTiles(map, start_data));


            //SaveTileMeshes(map, tiles);
            //AssetDatabase.Refresh();
            //Debug.Log("모든 Temp 오브젝트의 Init 호출이 병렬로 완료되었습니다.");

            //// for test
            //Vector3 grid_pivot;
            //Vector3 tile_pivot;
            //foreach (var grid in map.Values)
            //{ 
            //    foreach(var kvp in grid.MapNavDataDictionary)
            //    {
            //        // grid pivot
            //        grid_pivot = EditMapUtil.GetGridPivot(grid.gridKey);

            //        // tile pivot
            //        tileKey = kvp.Key;
            //        tile_pivot = EditMapUtil.GetTilePivot(grid.gridKey, kvp.Key);

            //        Debug.Log($"Grid_Pivot:{grid_pivot}, Tile_Pivot:{tile_pivot}");
            //    }
            //}

            Debug.Log($"--- End (length: {tiles.Length}) ---");
            System.GC.Collect();
        }
        private void LinkTiles(ConcurrentDictionary<int, EditMapGridData> map, float3 start_position)
        {
            Stack<float3> stack     = new Stack<float3>();
            HashSet<float3> visited = new HashSet<float3>();

            stack.Push(start_position);

            float3 target_pivot, neighbor_pivot;

            while (stack.Count > 0)
            {
                target_pivot = stack.Pop();
                if (true == visited.Contains(target_pivot))
                {
                    continue;
                }
                if (false == EditMapUtil.TryGetTileData(map, target_pivot, out EditMapTileData visit_tile))
                {
                    continue;
                }
                visited.Add(target_pivot);

                for (int i = dir.Length - 1; i >= 0; --i)
                {
                    for (int y = 0; y < ny.Length; ++y)
                    {
                        float3 dir = new float3(this.dir[i].x, ny[y], this.dir[i].y);
                        neighbor_pivot = target_pivot + dir;

                        // 타일 정보가 없으면 이어서 탐색한다.
                        if (false == EditMapUtil.TryGetTileData(map, neighbor_pivot, out EditMapTileData neighbor_tile))
                        {
                            continue;
                        }
                        // 이미 방문한 곳도 넘어간다.
                        if (true == visited.Contains(neighbor_pivot))
                        {
                            continue;
                        }

                        // 서로 연결되었다면 서로 연결 정보 추가하고 + 다음 방문을 예약한다.
                        if (true == visit_tile.TryGetLinkMask(map, neighbor_tile, dir, out int my_link_mask, out int neighbor_link_mask))
                        {
                            visit_tile = new EditMapTileData(visit_tile, my_link_mask);
                            map[visit_tile.GridKey].Data[visit_tile.TileKey] = visit_tile;

                            neighbor_tile = new EditMapTileData(neighbor_tile, neighbor_link_mask);
                            map[neighbor_tile.GridKey].Data[neighbor_tile.TileKey] = neighbor_tile;

                            stack.Push(neighbor_pivot);
                            break;
                        }
                    }
                }
            }
        }
    }
    public partial class MapTileSampling
    {
        private void SaveTileMeshes(ConcurrentDictionary<int, MapGridData> map, EditMapData[] tiles)
        {
            Dictionary<long, TempData> tempDataDict = new Dictionary<long, TempData>();
            EditMapData tile;
            TempData tempData;

            for (int i = 0; i < tiles.Length; ++i)
            {
                tile = tiles[i];
                long key = tile.RenderLayer << 32 | sceneIndex << 24 | tile.GridKey;

                if (false == tempDataDict.ContainsKey(key))
                {
                    tempDataDict[key] = new TempData
                    {
                        combineInstances = new List<CombineInstance>(),
                        combinedUVs = new List<Vector2>(),
                        vertexCount = 0,
                        index = 0
                    };
                }

                tempData = tempDataDict[key];

                int currentVertexCount = tempData.vertexCount;
                int tileVertexCount = tile.MeshFilter.sharedMesh.vertexCount;

                if (currentVertexCount + tileVertexCount > VERTEX_LIMIT)
                {
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.CombineMeshes(tempData.combineInstances.ToArray(), true, true);
                    combinedMesh.uv = tempData.combinedUVs.ToArray();

                    // VERTEX 꽉 채워졌으면 중도에 하나 생성하고
                    SaveMesh(map, combinedMesh, sceneIndex, tile.GridKey, tile.NaviLayer, tempData.index, true, false);

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

            // 마지막까지 남은 데이터를 마저 생성하는거네
            foreach (var kvp in tempDataDict)
            {
                tempData = kvp.Value;
                if (tempData.combineInstances.Count > 0)
                {
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.CombineMeshes(tempData.combineInstances.ToArray(), true, true);
                    combinedMesh.uv = tempData.combinedUVs.ToArray();

                    // long key = tile.RenderLayer << 32 | sceneIndex << 24 | tile.GridKey;
                    int gridKey = (int)(kvp.Key & 0x00FF_FFFF);
                    int layerMask = (int)(kvp.Key >> 32);
                    SaveMesh(map, combinedMesh, sceneIndex, gridKey, layerMask, tempData.index, true, false);
                }
            }

            EditorUtility.SetDirty(AddressableAssetSettingsDefaultObject.Settings);
        }
        private void SaveMesh(ConcurrentDictionary<int, MapGridData> map, Mesh mesh, int sceneIndex, int gridKey, int layer, int index, bool makeNewInstance, bool optimizeMesh)
        {
            if (false == map.ContainsKey(gridKey))
            {
                map.TryAdd(gridKey, new MapGridData(gridKey));
            }

            string assetName = $"MapRender_{sceneIndex}_G{gridKey}_L{layer}_{index}";
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

            // save data
            foreach (var grid in map)
            {
                AssetManager.WriteBinaryFile<MapGridData>(data: grid.Value,
                                                         dataPath: MAP_NAVI_DATA_PATH,
                                                         fileName: $"MapNavi_{grid.Key}",
                                                         addressableGroup: "MapNavi");
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
            int altasWidth = 2048;
            int altasHeight = 2048;

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
            const byte shiftTileLayer = 23;
            const byte shiftIsHalfScale = 22;
            const byte shiftTileX = 14;
            const byte shiftTileY = 8;
            const byte shiftTileZ = 0;

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
                var layer = (key >> shiftTileLayer) & 1;
                var scale = (key >> shiftIsHalfScale) & 1;

                var x = (key >> shiftTileX) & 0xFF;
                var y = (key >> shiftTileY) & 0x0F;
                var z = (key >> shiftTileZ) & 0xFF;

                Debug.Log($"[layer:{layer}][scale:{scale}][{x},{y},{z}]  [navi:{data.MapNavDataDictionary[key].navi}]");
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