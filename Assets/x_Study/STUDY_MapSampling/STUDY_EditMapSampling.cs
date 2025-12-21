#if UNITY_EDITOR
namespace Study.MapSampling
{
    using Script.Data;
    using Script.Editor.MapSampling;
    using Script.Manager;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    
    public partial class STUDY_EditMapSampling
    {
        private class CombineMeshData
        {
            public List<CombineInstance> combineInstances;
            public List<Vector2> combinedUVs;
            public int vertexCount;
            public int index;
        }

        private readonly string MAP_NAVI_DATA_PATH = "Rcs\\Bin\\MapNavRawData";

        // tile link를 할 때에 탐색 순서가 지정되어 있음!
        private readonly float[] DIFF_Y = new float[] { 0, 1, -1 };
        private readonly float2[] LINK_DIR = new float2[] 
        {
            new float2(1, -1), new float2(1, 1), new float2(-1, 1), new float2(-1, -1),
            new float2(0, -1), new float2(1, 0), new float2(0, 1),  new float2(-1, 0)
        };


        //[SerializeField] private Transform instanceTransform;
        //[SerializeField] private byte sceneIndex = 0;
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

            EditMapData[] tiles = instanceTransform.GetComponentsInChildren<EditMapData>(true);
            int length = tiles.Length;
            Allocator allocationType = Allocator.TempJob;

            var nativeSceneIndex  = new NativeArray<byte>  (length, allocationType);
            var nativeRenderLayer = new NativeArray<ushort>(length, allocationType);
            var nativePosition    = new NativeArray<float3>(length, allocationType);
            var nativeRotateY     = new NativeArray<float> (length, allocationType);
            var nativeHeights     = new NativeArray<ulong> (length, allocationType);
            var nativeResult      = new NativeArray<EditMapTileData>(length, allocationType);

            EditMapData tileObject; // TODO: EditMapTileObject 식으로 이름 바꿔야겠네.
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

            while (stack.Count > 0)
            {
                long targetID = stack.Pop();

                // 타일을 찾을 수 없음;
                if (false == EditMapUtil.TryGetTileData(map, targetID, out EditMapTileData visitTile))
                {
                    continue;
                }

                for (int i = 0; i < LINK_DIR.Length; ++i)
                {
                    // 이미 연결;
                    if (true == visitTile.IsLinked(LINK_DIR[i]))
                    {
                        continue;
                    }

                    for (int y = 0; y < DIFF_Y.Length; ++y)
                    {
                        float3 targetPivot = EditMapUtil.ComputeWorldPosition(targetID);
                        float3 targetDir = new float3(LINK_DIR[i].x, DIFF_Y[y], LINK_DIR[i].y);
                        long neighborID = EditMapUtil.ComputeID(targetPivot + targetDir);

                        // 이미 방문; 견적 다 나왔으니 넘어간다.
                        if (true == visited.Contains(neighborID))
                        {
                            break;
                        }

                        // 해당 위치에 타일이 없다? y값을 바꿔서 다시 탐색
                        if (false == EditMapUtil.TryGetTileData(map, neighborID, out EditMapTileData neighborTile))
                        {
                            continue;
                        }

                        // 이웃을 찾았다면?
                        visited.Add(neighborID); // 방문 목록에 추가
                        stack.Push(neighborID);  // 다음 탐색 대상으로 추가

                        // 이웃과 '연결' 되었는가? .LinkMask 갱신
                        if (true == visitTile.TryGetLinkMask(map, neighborTile, targetDir, out int myLinkMask, out int neighborLintMask))
                        {
                            EditMapUtil.ComputeKey(visitTile.ID, out int gKey, out int tKey);
                            visitTile = new EditMapTileData(visitTile, myLinkMask);
                            map[gKey].Data[tKey] = visitTile;

                            EditMapUtil.ComputeKey(neighborTile.ID, out gKey, out tKey);
                            neighborTile = new EditMapTileData(neighborTile, neighborLintMask);
                            map[gKey].Data[tKey] = neighborTile;

                            break;
                        }
                    }
                }
            }
        }

    }
}
#endif