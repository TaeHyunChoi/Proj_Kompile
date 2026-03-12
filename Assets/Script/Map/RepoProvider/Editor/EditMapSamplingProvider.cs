#if UNITY_EDITOR
namespace Script.Map.Provider
{
    using Script.Map.Data;
    using Script.Map.Utility;
    using Script.Asset.Provider;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public partial class EditMapSamplingRepoProvider 
    {
        private readonly string MAP_NAVI_DATA_PATH = "Rcs\\Bytes\\MapNavi";
        private readonly float[] DIFF_Y = new float[] { 0, 1, -1 };

        private readonly float2[] LINK_DIR = new float2[]
        {
            new float2(1, -1), new float2(1, 1), new float2(-1, 1), new float2(-1, -1), new float2(0, -1),
            new float2(1, 0), new float2(0, 1), new float2(-1, 0)
        };

        private const int VERTEX_LIMIT = 65536;
        private const int BATCH_TILE_LIMIT = 512;
        private const int BATCH_VERTEX_TARGET = 200000;
        private const float SPRITE_SIZE = 256f;
        private const int ATLAS_WIDTH = 2048;
        private const int ATLAS_HEIGHT = 2048;
        private const string SAVE_PATH_ROOT = "Assets/Rcs/MapRender";
        private static readonly string PROGRESS_BAR_TITLE = "Bake Map - Combining Meshes";

        // Pooling Objects
        private static EditBakeContext cachedContext;
        private static readonly Stack<EditGroupAccumulatorData> accmPool = new Stack<EditGroupAccumulatorData>();
        private static readonly Stack<EditMapTileChunkData> chunkPool = new Stack<EditMapTileChunkData>();

        private byte sceneIndex = 0;
        private ConcurrentDictionary<int, EditMapGridData> map;

        public void Bake()
        {
            Debug.Log($"Start Bake Map");

            // [Framework] MonoBehaviour 상속 객체는 Component 명명 규칙 사용
            var instance = Object.FindFirstObjectByType<EditMapSamplingComponent>(); 
            var instanceTransform = instance.transform;
            sceneIndex = instance.SceneIndex;

            // [Framework] Entity 명칭 대신 Component 명칭 사용
            EditMapTileComponent[] tiles = instanceTransform.GetComponentsInChildren<EditMapTileComponent>(true);
            int length = tiles.Length;
            Allocator allocationType = Allocator.TempJob;

            var nativeSceneIndex = new NativeArray<byte>(length, allocationType);
            var nativeRenderLayer = new NativeArray<ushort>(length, allocationType);
            var nativePosition = new NativeArray<float3>(length, allocationType);
            var nativeRotateY = new NativeArray<float>(length, allocationType);
            var nativeHeights = new NativeArray<ulong>(length, allocationType);
            var nativeResult = new NativeArray<EditMapTileData>(length, allocationType);

            EditMapTileComponent tileComponent;
            for (int i = 0; i < tiles.Length; ++i)
            {
                tileComponent = tiles[i];

                nativeSceneIndex[i] = sceneIndex;
                nativeRenderLayer[i] = tileComponent.RenderLayer;

                int x = Mathf.FloorToInt(tileComponent.transform.position.x);
                int y = Mathf.FloorToInt(tileComponent.transform.position.y);
                int z = Mathf.FloorToInt(tileComponent.transform.position.z);
                nativePosition[i] = new float3(x, y, z);

                nativeRotateY[i] = Mathf.FloorToInt(tileComponent.transform.eulerAngles.y);
                nativeHeights[i] = tileComponent.HeightMask;
            }

            // [Framework] Utility성 Job. Burst Compile 적용 대상
            EditMapTileJobUtil job = new EditMapTileJobUtil
            {
                SceneIndex = nativeSceneIndex,
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
            int[] computedGridKeys = new int[length];

            map = new ConcurrentDictionary<int, EditMapGridData>();
            for (int i = 0; i < nativeResult.Length; ++i)
            {
                MapCoordUtil.ComputeKey(nativeResult[i].ID, out int gridKey, out int tileKey);
                naviMask = nativeResult[i].NaviMask;
                renderIndex = nativeResult[i].RenderIndex;

                computedGridKeys[i] = gridKey;

                if (false == map.ContainsKey(gridKey))
                {
                    map.TryAdd(gridKey, new EditMapGridData(gridKey));
                }

                EditMapTileData tileData = new EditMapTileData()
                {
                    ID = nativeResult[i].ID, NaviMask = naviMask, LinkMask = default, RenderIndex = renderIndex
                };
                map[gridKey].TryAdd(tileKey, tileData);
            }

            // ## Dispose NativeArray
            nativeSceneIndex.Dispose();
            nativeRenderLayer.Dispose();
            nativePosition.Dispose();
            nativeRotateY.Dispose();
            nativeHeights.Dispose();
            nativeResult.Dispose();

            // ## Set Grid Data & Combine Mesh
            LinkTiles(map);
            CombineAndRegister(map, tiles, computedGridKeys, sceneIndex, "MapRender");

            // ## Save Data.bin
            string fullNaviPath = $"Assets/{MAP_NAVI_DATA_PATH.Replace('\\', '/')}";
            if (true == AssetDatabase.IsValidFolder(fullNaviPath))
            {
                AssetDatabase.DeleteAsset(fullNaviPath);
            }

            if (false == System.IO.Directory.Exists(fullNaviPath))
            {
                System.IO.Directory.CreateDirectory(fullNaviPath);
            }

            AssetDatabase.Refresh();

            foreach (KeyValuePair<int, EditMapGridData> grid in map)
            {
                MapGridData mapGridData = new MapGridData()
                {
                    Key = grid.Key,
                    NaviTileDict = grid.Value.ParseData(),
                    layerMeshAssets = grid.Value.LayerMeshAssets
                };

                    AssetRepoProvider.WriteBinaryFile<MapGridData>(
                    data: mapGridData,
                    relativePath: MAP_NAVI_DATA_PATH,
                    fileName: $"MapNavi_{mapGridData.Key}",
                    addressableGroup: "MapNavi",
                    addressableLabel: "MapNavi"
                );
            }

            Debug.Log($"End Bake (length: {tiles.Length})");
            System.GC.Collect();
        }

        private void LinkTiles(ConcurrentDictionary<int, EditMapGridData> map)
        {
            List<EditMapTileData> allTiles = new List<EditMapTileData>();
            foreach (var grid in map.Values)
            {
                foreach (var tile in grid.Data.Values)
                {
                    allTiles.Add(tile);
                }
            }

            int count = allTiles.Count;
            if (0 == count) return;

            var allocType = Allocator.TempJob;
            var keyArray = new NativeArray<long>(count, allocType);
            var tileMap = new NativeHashMap<long, EditMapTileData>(count, allocType);
            var linkDirs = new NativeArray<float2>(LINK_DIR, allocType);
            var diffYs = new NativeArray<float>(DIFF_Y, allocType);
            var jobResult = new NativeArray<EditMapTileData>(count, allocType);

            for (int i = 0; i < count; ++i)
            {
                keyArray[i] = allTiles[i].ID;
                tileMap.TryAdd(allTiles[i].ID, allTiles[i]);
            }

            EditMapLinkJobUtil linkJob = new EditMapLinkJobUtil
            {
                KeyArray = keyArray,
                Map = tileMap,
                Results = jobResult,
                LinkDirs = linkDirs,
                DiffYs = diffYs
            };
            JobHandle handle = linkJob.Schedule(count, 64);
            handle.Complete();

            for (int i = 0; i < count; ++i)
            {
                EditMapTileData resultTile = jobResult[i];
                MapCoordUtil.ComputeKey(resultTile.ID, out int gKey, out int tKey);

                if (true == map.TryGetValue(gKey, out var gridData))
                {
                    gridData.Data[tKey] = resultTile;
                }
            }

            keyArray.Dispose();
            tileMap.Dispose();
            linkDirs.Dispose();
            diffYs.Dispose();
            jobResult.Dispose();

            Debug.Log($"LinkTiles Job Completed: {count} tiles processed.");
        }

        public static void CombineAndRegister(ConcurrentDictionary<int, EditMapGridData> map,
            EditMapTileComponent[] tiles, // [Framework] 명칭 변경 반영
            int[] gridKeys, 
            int sceneIndex,
            string adderessableGroupName)
        {
            if (null == tiles || 0 == tiles.Length)
            {
                Debug.LogWarning("No tiles to process;");
                return;
            }

            if (AssetDatabase.IsValidFolder(SAVE_PATH_ROOT))
            {
                AssetDatabase.DeleteAsset(SAVE_PATH_ROOT);
            }

            if (!System.IO.Directory.Exists(SAVE_PATH_ROOT))
            {
                System.IO.Directory.CreateDirectory(SAVE_PATH_ROOT);
            }

            AssetDatabase.Refresh();

            if (null == cachedContext)
            {
                cachedContext = new EditBakeContext();
            }

            cachedContext.Setup(sceneIndex, map, adderessableGroupName);

            var accumulators = new Dictionary<EditMapGroupKey, EditGroupAccumulatorData>();
            int totalTiles = tiles.Length;
            bool userCancelled = false;

            try
            {
                int start = 0;
                List<int> batchIndices = new List<int>();
                while (start < totalTiles)
                {
                    if (true == EditorUtility.DisplayCancelableProgressBar(PROGRESS_BAR_TITLE,
                            $"Processing {start}/{totalTiles}",
                            (float)start / totalTiles))
                    {
                        userCancelled = true;
                        break;
                    }

                    batchIndices.Clear();
                    int currentBatchVertex = 0;
                    int idx = start;

                    while (idx < totalTiles && batchIndices.Count < BATCH_TILE_LIMIT)
                    {
                        EditMapTileComponent tile = tiles[idx]; // [Framework] 명칭 변경 반영
                        int vc = 0;
                        if (true == tile.TryGetSharedMesh(out Mesh tileMesh))
                        {
                            vc = tileMesh.vertexCount;
                        }

                        if (BATCH_VERTEX_TARGET < currentBatchVertex + vc && 0 < batchIndices.Count)
                        {
                            break;
                        }

                        batchIndices.Add(idx);
                        currentBatchVertex += vc;
                        ++idx;
                    }

                    start += batchIndices.Count;
                    ProcessBatch(cachedContext, tiles, gridKeys, batchIndices, accumulators);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (var kv in accumulators)
            {
                EditMapGroupKey key = kv.Key;
                EditGroupAccumulatorData accm = kv.Value;

                while (0 < accm.Tiles.Count)
                {
                    FlushAccumulatorPart(cachedContext, key, accm);
                }

                accm.Clear();
                accmPool.Push(accm);
            }

            accumulators.Clear();

            RegisterAddressables(cachedContext);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(true == userCancelled ? "Bake cancelled by user" : "Bake Completed successfully");
        }

        private static void ProcessBatch(EditBakeContext ctx, EditMapTileComponent[] tilesInGrid, int[] gridKeys,
            List<int> indices, Dictionary<EditMapGroupKey, EditGroupAccumulatorData> accums)
        {
            EditMapTileComponent tile; // [Framework] 명칭 변경 반영
            foreach (int i in indices)
            {
                tile = tilesInGrid[i];
                if (false == tile.TryGetSharedMesh(out Mesh mesh))
                {
                    continue;
                }

                int vc = mesh.vertexCount;
                if (0 == vc)
                {
                    continue;
                }

                int correctGridKey = gridKeys[i];

                EditMapTileChunkData chunkData = (0 < chunkPool.Count) ? chunkPool.Pop() : new EditMapTileChunkData();
                chunkData.Instance = new CombineInstance { mesh = mesh, transform = tile.transform.localToWorldMatrix };
                chunkData.UVs = CalculateAtlasUVs(mesh, tile.TextureIndex);
                chunkData.VertexCount = vc;
                chunkData.RenderLayer = tile.RenderLayer;
                chunkData.GridKey = correctGridKey; 

                EditMapGroupKey key = new EditMapGroupKey(tile.RenderLayer, correctGridKey); 
                if (false == accums.TryGetValue(key, out EditGroupAccumulatorData acc))
                {
                    acc = 0 < accmPool.Count ? accmPool.Pop() : new EditGroupAccumulatorData();
                    acc.Clear();
                    accums[key] = acc;
                }

                acc.Tiles.Enqueue(chunkData);
                acc.VertexSum += vc;

                while (acc.VertexSum > VERTEX_LIMIT)
                {
                    FlushAccumulatorPart(ctx, key, acc);
                }
            }
        }

        private static Vector2[] CalculateAtlasUVs(Mesh mesh, int textureIndex)
        {
            Vector3[] verts = mesh.vertices;
            Vector2[] uvs = new Vector2[verts.Length];

            int atlasCols = ATLAS_WIDTH / ATLAS_HEIGHT;
            float uvW = SPRITE_SIZE / ATLAS_WIDTH;
            float uvH = SPRITE_SIZE / ATLAS_HEIGHT;

            float baseX = (textureIndex % atlasCols) * uvW;
            float baseY = (textureIndex / atlasCols) * uvH;

            float x, y;
            for (int i = 0; i < verts.Length; ++i)
            {
                x = baseX + verts[i].x * uvW;
                y = baseY + verts[i].y * uvH;
                uvs[i] = new Vector2(x, y);
            }

            return uvs;
        }

        private static void FlushAccumulatorPart(EditBakeContext ctx, EditMapGroupKey key, EditGroupAccumulatorData acc)
        {
            if (0 == acc.Tiles.Count)
            {
                return;
            }

            List<CombineInstance> takeInstances = new List<CombineInstance>();
            List<Vector2> takeUVs = new List<Vector2>();
            int takenVerts = 0;
            int tilesConsumed = 0;

            foreach (EditMapTileChunkData chunk in acc.Tiles)
            {
                if (0 < takenVerts && VERTEX_LIMIT < takenVerts + chunk.VertexCount)
                {
                    break;
                }

                takeInstances.Add(chunk.Instance);
                takeUVs.AddRange(chunk.UVs);
                takenVerts += chunk.VertexCount;
                ++tilesConsumed;

                if (VERTEX_LIMIT < takenVerts)
                {
                    break;
                }
            }

            SaveMeshAsset(ctx, key, acc.PartIndex, takeInstances.ToArray(), takeUVs.ToArray());

            for (int i = 0; i < tilesConsumed; ++i)
            {
                var removed = acc.Tiles.Dequeue();
                acc.VertexSum -= removed.VertexCount;

                removed.Clear();
                chunkPool.Push(removed);
            }

            ++acc.PartIndex;
        }

        private static void SaveMeshAsset(EditBakeContext ctx, EditMapGroupKey key, int partIdx,
            CombineInstance[] instances, Vector2[] uvs)
        {
            string assetName = $"MapRender_{ctx.SceneIndex}_G{key.GridKey}_L{key.RenderLayer}_{partIdx}";
            string path = $"{SAVE_PATH_ROOT}/{assetName}.asset";

            Mesh combinedMesh = new Mesh();
            try
            {
                if (VERTEX_LIMIT < uvs.Length)
                {
                    combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }

                combinedMesh.CombineMeshes(instances, true, true);
                combinedMesh.uv = uvs;

                MeshUtility.Optimize(combinedMesh);

                if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }

                AssetDatabase.CreateAsset(combinedMesh, path);

                if (null != ctx.Map)
                {
                    EditMapGridData gridData = ctx.Map.GetOrAdd(key.GridKey, k => new EditMapGridData(k));
                    gridData.AddAssetFile(assetName);
                    gridData.AddMeshAsset(key.RenderLayer, assetName);
                }

                ctx.CreatedAssets.Add((path, assetName));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save mesh {assetName}: {e.Message}");
            }
        }

        private static void RegisterAddressables(EditBakeContext ctx)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (null == settings || 0 == ctx.CreatedAssets.Count)
            {
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(ctx.AddressableGroupName);
            if (null == group)
            {
                return;
            }

            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            foreach ((string path, string assetName) in ctx.CreatedAssets)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (true == string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.SetAddress(assetName);
                entries.Add(entry);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries.ToArray(), true);
        }
    }
}
#endif