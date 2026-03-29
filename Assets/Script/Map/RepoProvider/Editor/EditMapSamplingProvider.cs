#if UNITY_EDITOR
namespace Script.Map.Provider
{
    using Script.Map.Data;
    using Script.Map.Utility;
    using Script.Map.Entity;
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

        // [핵심 변경] 머티리얼 생성을 위해 텍스처 원본 참조를 일시적으로 보관합니다.
        private struct BakeGroupKey : IEquatable<BakeGroupKey>
        {
            public ushort RenderLayer;
            public int GridKey;
            public string TopAtlas;
            public string SideAtlas;
            public Texture2D TopTexRef;  // 머티리얼 자동 생성용 참조
            public Texture2D SideTexRef; // 머티리얼 자동 생성용 참조

            public bool Equals(BakeGroupKey other) =>
                RenderLayer == other.RenderLayer && GridKey == other.GridKey && TopAtlas == other.TopAtlas && SideAtlas == other.SideAtlas;

            public override int GetHashCode() => HashCode.Combine(RenderLayer, GridKey, TopAtlas, SideAtlas);
        }

        private class BakeChunkData
        {
            public CombineInstance Instance;
            public int VertexCount;
            public ushort RenderLayer;
            public int GridKey;
            public int TopTextureIndex;
            public int SideTextureIndex;
        }

        private class BakeAccumulator
        {
            public int VertexSum;
            public int PartIndex;
            public Queue<BakeChunkData> Tiles = new Queue<BakeChunkData>();
            public void Clear() { VertexSum = 0; Tiles.Clear(); }
        }

        private static EditBakeContext cachedContext;
        private static readonly Stack<BakeAccumulator> accmPool = new Stack<BakeAccumulator>();
        private static readonly Stack<BakeChunkData> chunkPool = new Stack<BakeChunkData>();

        private byte sceneIndex = 0;
        private ConcurrentDictionary<int, EditMapGridData> map;

        public void Bake()
        {
            Debug.Log($"Start Bake Map");

            var instance = Object.FindFirstObjectByType<EditMapSamplingComponent>();
            if (instance == null) return;

            var instanceTransform = instance.transform;
            sceneIndex = instance.SceneIndex;

            EditMapTileComponent[] tiles = instanceTransform.GetComponentsInChildren<EditMapTileComponent>(true);
            int length = tiles.Length;
            Allocator allocationType = Allocator.TempJob;

            var nativeSceneIndex = new NativeArray<byte>(length, allocationType);
            var nativeRenderLayer = new NativeArray<ushort>(length, allocationType);
            var nativePosition = new NativeArray<float3>(length, allocationType);
            var nativeHeights = new NativeArray<ulong>(length, allocationType);
            var nativeResult = new NativeArray<EditMapTileData>(length, allocationType);

            for (int i = 0; i < tiles.Length; ++i)
            {
                EditMapTileComponent tileComponent = tiles[i];

                nativeSceneIndex[i] = sceneIndex;
                nativeRenderLayer[i] = tileComponent.RenderLayer;

                int x = Mathf.FloorToInt(tileComponent.transform.position.x);
                int y = Mathf.FloorToInt(tileComponent.transform.position.y);
                int z = Mathf.FloorToInt(tileComponent.transform.position.z);
                nativePosition[i] = new float3(x, y, z);
                nativeHeights[i] = tileComponent.HeightMask;
            }

            EditMapTileJobUtil job = new EditMapTileJobUtil
            {
                SceneIndex = nativeSceneIndex,
                RenderLayer = nativeRenderLayer,
                Position = nativePosition,
                Height = nativeHeights,
                Data = nativeResult
            };
            JobHandle jobHandle = job.Schedule(tiles.Length, 64);
            jobHandle.Complete();

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
                    ID = nativeResult[i].ID,
                    NaviMask = naviMask,
                    LinkMask = default,
                    RenderIndex = renderIndex
                };
                map[gridKey].TryAdd(tileKey, tileData);
            }

            nativeSceneIndex.Dispose();
            nativeRenderLayer.Dispose();
            nativePosition.Dispose();
            nativeHeights.Dispose();
            nativeResult.Dispose();

            LinkTiles(map);
            CombineAndRegister(map, tiles, computedGridKeys, sceneIndex, "MapRender");

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
            EditMapTileComponent[] tiles,
            int[] gridKeys,
            int sceneIndex,
            string adderessableGroupName)
        {
            if (null == tiles || 0 == tiles.Length) return;

            if (AssetDatabase.IsValidFolder(SAVE_PATH_ROOT)) AssetDatabase.DeleteAsset(SAVE_PATH_ROOT);
            if (!System.IO.Directory.Exists(SAVE_PATH_ROOT)) System.IO.Directory.CreateDirectory(SAVE_PATH_ROOT);

            AssetDatabase.Refresh();

            if (null == cachedContext) cachedContext = new EditBakeContext();
            cachedContext.Setup(sceneIndex, map, adderessableGroupName);

            var accumulators = new Dictionary<BakeGroupKey, BakeAccumulator>();
            int totalTiles = tiles.Length;
            bool userCancelled = false;

            try
            {
                int start = 0;
                List<int> batchIndices = new List<int>();
                while (start < totalTiles)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(PROGRESS_BAR_TITLE, $"Processing {start}/{totalTiles}", (float)start / totalTiles))
                    {
                        userCancelled = true;
                        break;
                    }

                    batchIndices.Clear();
                    int currentBatchVertex = 0;
                    int idx = start;

                    while (idx < totalTiles && batchIndices.Count < BATCH_TILE_LIMIT)
                    {
                        EditMapTileComponent tile = tiles[idx];
                        int vc = 0;
                        if (tile.TryGetSharedMesh(out Mesh tileMesh)) vc = tileMesh.vertexCount;

                        if (BATCH_VERTEX_TARGET < currentBatchVertex + vc && 0 < batchIndices.Count) break;

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
                BakeGroupKey key = kv.Key;
                BakeAccumulator accm = kv.Value;

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

            Debug.Log(userCancelled ? "Bake cancelled by user" : "Bake Completed successfully");
        }

        private static void ProcessBatch(EditBakeContext ctx, EditMapTileComponent[] tilesInGrid, int[] gridKeys,
            List<int> indices, Dictionary<BakeGroupKey, BakeAccumulator> accums)
        {
            foreach (int i in indices)
            {
                EditMapTileComponent tile = tilesInGrid[i];
                if (!tile.TryGetSharedMesh(out Mesh mesh)) continue;

                int vc = mesh.vertexCount;
                if (vc == 0) continue;

                int correctGridKey = gridKeys[i];

                string topAtlasName = tile.TopAtlasTexture != null ? tile.TopAtlasTexture.name : "None";
                string sideAtlasName = tile.SideAtlasTexture != null ? tile.SideAtlasTexture.name : "None";

                // [핵심 변경] 머티리얼 자동 생성을 위해 원본 텍스처 참조를 GroupKey에 담아 전달합니다.
                BakeGroupKey key = new BakeGroupKey
                {
                    RenderLayer = tile.RenderLayer,
                    GridKey = correctGridKey,
                    TopAtlas = topAtlasName,
                    SideAtlas = sideAtlasName,
                    TopTexRef = tile.TopAtlasTexture,
                    SideTexRef = tile.SideAtlasTexture
                };

                BakeChunkData chunkData = (0 < chunkPool.Count) ? chunkPool.Pop() : new BakeChunkData();
                chunkData.Instance = new CombineInstance { mesh = mesh, transform = tile.transform.localToWorldMatrix };
                chunkData.VertexCount = vc;
                chunkData.RenderLayer = tile.RenderLayer;
                chunkData.GridKey = correctGridKey;

                chunkData.TopTextureIndex = tile.TopTextureIndex;
                chunkData.SideTextureIndex = tile.SideTextureIndex;

                if (!accums.TryGetValue(key, out BakeAccumulator acc))
                {
                    acc = 0 < accmPool.Count ? accmPool.Pop() : new BakeAccumulator();
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

        private static void FlushAccumulatorPart(EditBakeContext ctx, BakeGroupKey key, BakeAccumulator acc)
        {
            if (0 == acc.Tiles.Count) return;

            List<CombineInstance> takeInstances = new List<CombineInstance>();
            List<Mesh> tempMeshes = new List<Mesh>();
            int takenVerts = 0;
            int tilesConsumed = 0;
            float uvStep = 1f / 8f;

            foreach (BakeChunkData chunk in acc.Tiles)
            {
                if (0 < takenVerts && VERTEX_LIMIT < takenVerts + chunk.VertexCount) break;

                Mesh tempMesh = Object.Instantiate(chunk.Instance.mesh);
                int vc = tempMesh.vertexCount;

                Vector2[] uv2 = new Vector2[vc];
                Vector2[] uv3 = new Vector2[vc];

                int tLocal = chunk.TopTextureIndex % 64;
                Vector2 tOffset = new Vector2((tLocal % 8) * uvStep, 1.0f - ((tLocal / 8 + 1) * uvStep));

                int sLocal = chunk.SideTextureIndex % 64;
                Vector2 sOffset = new Vector2((sLocal % 8) * uvStep, 1.0f - ((sLocal / 8 + 1) * uvStep));

                for (int j = 0; j < vc; j++)
                {
                    uv2[j] = tOffset;
                    uv3[j] = sOffset;
                }

                tempMesh.uv2 = uv2;
                tempMesh.uv3 = uv3;
                tempMeshes.Add(tempMesh);

                CombineInstance ci = chunk.Instance;
                ci.mesh = tempMesh;
                takeInstances.Add(ci);

                takenVerts += chunk.VertexCount;
                ++tilesConsumed;

                if (VERTEX_LIMIT < takenVerts) break;
            }

            SaveMeshAsset(ctx, key, acc.PartIndex, takeInstances.ToArray());

            foreach (var tm in tempMeshes) Object.DestroyImmediate(tm);

            for (int i = 0; i < tilesConsumed; ++i)
            {
                var removed = acc.Tiles.Dequeue();
                acc.VertexSum -= removed.VertexCount;
                chunkPool.Push(removed);
            }

            ++acc.PartIndex;
        }

        private static void SaveMeshAsset(EditBakeContext ctx, BakeGroupKey key, int partIdx, CombineInstance[] instances)
        {
            // =================================================================================
            // [나으리 아이디어 구현부] 머티리얼(Material) 에셋 자동 생성 및 등록
            // =================================================================================
            string matName = $"Mat_{key.TopAtlas}_{key.SideAtlas}";
            string matPath = $"{SAVE_PATH_ROOT}/{matName}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Custom/WorldSpaceAtlasShader_v4");
                if (shader != null)
                {
                    mat = new Material(shader);
                    if (key.TopTexRef != null) mat.SetTexture("_TopAtlas", key.TopTexRef);
                    if (key.SideTexRef != null) mat.SetTexture("_SideAtlas", key.SideTexRef);

                    AssetDatabase.CreateAsset(mat, matPath);
                    // 생성된 머티리얼도 Addressable 시스템에 자동 등록시킵니다.
                    ctx.CreatedAssets.Add((matPath, matName));
                }
                else
                {
                    Debug.LogWarning($"[Framework] 'Custom/WorldSpaceAtlasShader_v4' 쉐이더를 찾을 수 없어 {matName} 생성을 건너뜁니다.");
                }
            }

            // =================================================================================
            // 메쉬 저장 (기존 유지)
            // =================================================================================
            string assetName = $"MapRender_{ctx.SceneIndex}_G{key.GridKey}_L{key.RenderLayer}_{key.TopAtlas}_{key.SideAtlas}_{partIdx}";
            string path = $"{SAVE_PATH_ROOT}/{assetName}.asset";

            Mesh combinedMesh = new Mesh();
            try
            {
                int totalVerts = 0;
                foreach (var ci in instances) totalVerts += ci.mesh.vertexCount;

                if (VERTEX_LIMIT < totalVerts) combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                combinedMesh.CombineMeshes(instances, true, true);
                MeshUtility.Optimize(combinedMesh);

                if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
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
            if (null == settings || 0 == ctx.CreatedAssets.Count) return;

            AddressableAssetGroup group = settings.FindGroup(ctx.AddressableGroupName);
            if (null == group) return;

            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            foreach ((string path, string assetName) in ctx.CreatedAssets)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (true == string.IsNullOrEmpty(guid)) continue;

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.SetAddress(assetName);
                entries.Add(entry);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries.ToArray(), true);
        }
    }
}
#endif