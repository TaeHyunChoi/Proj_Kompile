#if UNITY_EDITOR
namespace Study.MapSampling
{
    using Script.Data;
    using Script.Editor.MapSampling;
    using Script.Manager;
    using Script.Map;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// partial: STUDY_EditMapSampling
    /// partial: STUDY_EditMapSampling_combineMesh.cs
    /// </summary>
    public partial class STUDY_EditMapSampling
    {
        private class CombineMeshData
        {
            public List<CombineInstance> combineInstances;
            public List<Vector2> combinedUVs;
            public int vertexCount;
            public int index;
        }

        private readonly string MAP_NAVI_DATA_PATH = "Rcs\\Bytes\\MapNavi";

        // tile link를 할 때에 탐색 순서가 지정되어 있음!
        private readonly float[] DIFF_Y = new float[] { 0, 1, -1 };
        private readonly float2[] LINK_DIR = new float2[] 
        {
            new float2(1, -1), new float2(1, 1), new float2(-1, 1), new float2(-1, -1),
            new float2(0, -1), new float2(1, 0), new float2(0, 1),  new float2(-1, 0)
        };

        private byte sceneIndex = 0;

        private ConcurrentDictionary<int, EditMapGridData> map;
        public ConcurrentDictionary<int, EditMapGridData> Map => map;

        public void Bake()
        {
            Debug.Log($"Start Bake Map");

            // ## set data
            var instance = Object.FindFirstObjectByType<EditMapTileSampling>();
            var instanceTransform = instance.MapRoot;
            sceneIndex = instance.SceneIndex;

            EditMapTileObject[] tiles = instanceTransform.GetComponentsInChildren<EditMapTileObject>(true);
            int length = tiles.Length;
            Allocator allocationType = Allocator.TempJob;

            var nativeSceneIndex  = new NativeArray<byte>  (length, allocationType);
            var nativeRenderLayer = new NativeArray<ushort>(length, allocationType);
            var nativePosition    = new NativeArray<float3>(length, allocationType);
            var nativeRotateY     = new NativeArray<float> (length, allocationType);
            var nativeHeights     = new NativeArray<ulong> (length, allocationType);
            var nativeResult      = new NativeArray<EditMapTileData>(length, allocationType);

            EditMapTileObject tileObject; // TODO: EditMapTileObject 식으로 이름 바꿔야겠네.
            for (int i = 0; i < tiles.Length; ++i)
            {
                tileObject = tiles[i];

                nativeSceneIndex[i]  = sceneIndex;
                nativeRenderLayer[i] = tileObject.RenderLayer;

                int x = Mathf.FloorToInt(tileObject.transform.position.x);
                int y = Mathf.FloorToInt(tileObject.transform.position.y);
                int z = Mathf.FloorToInt(tileObject.transform.position.z);
                nativePosition[i] = new float3(x, y, z);

                nativeRotateY[i] = Mathf.FloorToInt(tileObject.transform.eulerAngles.y);
                nativeHeights[i] = tileObject.HeightMask;
            }

            EditMapTileJob job = new EditMapTileJob
            {
                SceneIndex  = nativeSceneIndex,
                RenderLayer = nativeRenderLayer,
                Position = nativePosition,
                RotY = nativeRotateY,
                Height = nativeHeights,
                Data = nativeResult
            };
            JobHandle jobHandle = job.Schedule(tiles.Length, 64);
            jobHandle.Complete();


            // ## Register Map
            ushort renderIndex;
            long naviMask;

            map = new ConcurrentDictionary<int, EditMapGridData>();
            for (int i = 0; i < nativeResult.Length; ++i)
            {
                EditMapUtil.ComputeKey(nativeResult[i].ID, out int gridKey, out int tileKey);
                naviMask = nativeResult[i].NaviMask;
                renderIndex = nativeResult[i].RenderIndex;

                if (false == map.ContainsKey(gridKey))
                {
                    map.TryAdd(gridKey, new EditMapGridData(gridKey));
                }

                EditMapTileData tileData = new EditMapTileData()
                {
                    ID          = nativeResult[i].ID, //EditMapUtil.ComputeID(gridKey, tileKey)로 굳이 같은 계산을 할 필요가?
                    NaviMask    = naviMask,
                    LinkMask    = default,            // Link() 단계에서 값 입력
                    RenderIndex = renderIndex
                };
                map[gridKey].TryAdd(tileKey, tileData);
            }

            long startID = nativeResult[0].ID;


            // ## Dispose NativeArray
            nativeSceneIndex.Dispose();
            nativeRenderLayer.Dispose();
            nativePosition.Dispose();
            nativeRotateY.Dispose();
            nativeHeights.Dispose();
            nativeResult.Dispose();


            // ## Set Grid Data
            LinkTiles(map, startID);
            CombineAndRegister(map, tiles, sceneIndex, "MapRender");   // **Streaming, 부분 처리 방식으로

            // ## Save Data.bin
            foreach (KeyValuePair<int, EditMapGridData> grid in map)
            {
                MapGridData mapGridData = new MapGridData()
                {
                    Key             = grid.Key,
                    NaviTileDict    = grid.Value.ParseData(),
                    layerMeshAssets = grid.Value.LayerMeshAssets
                };

                AssetManager.WriteBinaryFile<MapGridData>(
                    data: mapGridData,
                    dataPath: MAP_NAVI_DATA_PATH,
                    fileName: $"MapNavi_{mapGridData.Key}",
                    addressableGroup: "MapNavi"
                    );
            }

            Debug.Log($"End Bake (length: {tiles.Length})");
            System.GC.Collect();
        }
        private void LinkTiles(ConcurrentDictionary<int, EditMapGridData> map, long startID)
        {
            Stack<long> stack = new Stack<long>();
            HashSet<long> visited = new HashSet<long>();

            stack.Push(startID);
            visited.Add(startID);

            while (stack.Count > 0)
            {
                long targetID = stack.Pop();

                // 타일을 찾을 수 없음
                if (false == EditMapUtil.TryGetTileData(map, targetID, out EditMapTileData visitTile))
                {
                    Debug.LogWarning($"MapSampling: 타일을 찾을 수 없음({targetID}, {EditMapUtil.ComputeWorldPosition(targetID)})");
                    continue;
                }

                for (int i = 0; i < LINK_DIR.Length; ++i)
                {
                    // 이미 이 방향으로 링크가 되어 있다면 스킵
                    if (true == visitTile.IsLinked(LINK_DIR[i]))
                    {
                        continue;
                    }

                    for (int y = 0; y < DIFF_Y.Length; ++y)
                    {
                        float3 targetPivot = EditMapUtil.ComputeWorldPosition(targetID);
                        float3 targetDir = new float3(LINK_DIR[i].x, DIFF_Y[y], LINK_DIR[i].y);

                        long neighborID = EditMapUtil.ComputeID(targetPivot + targetDir);
                        //방문한 타일이라도 링크 연결은 확인해야 한다.

                        // 해당 위치에 타일이 없으면 다음 높이를 검색
                        if (false == EditMapUtil.TryGetTileData(map, neighborID, out EditMapTileData neighborTile))
                        {
                            continue;
                        }

                        // 이웃과 연결 가능한 상태인가?
                        if (true == visitTile.TryGetLinkMask(map, neighborTile, targetDir, out int myLinkMask, out int neighborLinkMask))
                        {
                            // 내 타일 데이터 갱신
                            visitTile = new EditMapTileData(visitTile, myLinkMask); // 갱신 후
                            EditMapUtil.ComputeKey(visitTile.ID, out int gKey, out int tKey); // gKey, tKey 찾아서
                            map[gKey].Data[tKey] = visitTile; // 데이터 갱신

                            // 이웃 타일 데이터 갱신
                            EditMapUtil.ComputeKey(neighborTile.ID, out gKey, out tKey);
                            neighborTile = new EditMapTileData(neighborTile, neighborLinkMask);
                            map[gKey].Data[tKey] = neighborTile;

                            // 여기서 visited를 체크:
                            // 이웃 타일이 아직 방문하지 않은 곳이라면 스택에 추가하여 나중에 그 타일 기준으로도 탐색
                            if (false == visited.Contains(neighborID))
                            {
                                visited.Add(neighborID);
                                stack.Push(neighborID);
                            }

                            // 연결을 성공했으므로 다음 높이를 볼 필요 없음 -> 다음 방향으로
                            break;
                        }
                    }
                }
            }
        }
    }
}
#endif