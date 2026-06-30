#if UNITY_EDITOR
namespace Kompile.Map.Editor.Provider
{
    using Kompile.Map.Data;
    using Kompile.Map.Utility;
    using Kompile.Map.Entity;
    using Kompile.Map.Editor.Data; 
    using Kompile.Map.Editor.Utility;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;

    using Object = UnityEngine.Object;

    public partial class EditMapSamplingProvider // main
    {
        // -- Path --
        private const string SAVE_PATH_ROOT = "Assets/Rcs/MapRender";
        private const string MAP_NAVI_DATA_PATH = "Rcs\\Bytes\\MapNavi";
        private const string SHADER_PATH = "Custom/WorldSpaceAtlasShader";

        // -- Height, Link -- 
        private readonly float[] DIFF_Y = new float[] { 0, 1, -1 };

        private readonly float2[] LINK_DIR = new float2[]
        {
            new float2(1, -1), new float2(1, 1), new float2(-1, 1), new float2(-1, -1), new float2(0, -1),
            new float2(1, 0), new float2(0, 1), new float2(-1, 0)
        };

        // -- Batch --
        private const int VERTEX_LIMIT = 65536;
        private const int BATCH_TILE_LIMIT = 512;
        private const int BATCH_VERTEX_TARGET = 200000;

        // -- Bake Context --
        private static EditBakeContext cachedContext;
        private static readonly Stack<EditBakeAccumulator> accmPool = new Stack<EditBakeAccumulator>();
        private static readonly Stack<EditBakeChunkData> chunkPool = new Stack<EditBakeChunkData>();

        // -- Map --
        private byte sceneIndex = 0;
        private ConcurrentDictionary<int, EditMapGridData> map;

        public void Bake()
        {
            Debug.Log($"Start Bake Map");

            var instance = Object.FindFirstObjectByType<EditMapSamplingComponent>();
            if (false == instance)
            {
                return;
            }

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
                SceneIndex  = nativeSceneIndex,
                RenderLayer = nativeRenderLayer,
                Position    = nativePosition,
                Height      = nativeHeights,
                Data        = nativeResult
            };
            JobHandle jobHandle = job.Schedule(tiles.Length, 64);
            jobHandle.Complete();

            int[] computedGridKeys = new int[length];

            map = new ConcurrentDictionary<int, EditMapGridData>();
            for (int i = 0; i < nativeResult.Length; ++i)
            {
                MapCoordUtil.ComputeKey(nativeResult[i].ID, out int gridKey, out int tileKey);

                computedGridKeys[i] = gridKey;
                long naviMask = nativeResult[i].NaviMask;
                ushort renderIndex = nativeResult[i].RenderIndex;

                if (false == map.ContainsKey(gridKey))
                {
                    map.TryAdd(gridKey, new EditMapGridData(gridKey));
                }

                EditMapTileData tileData = new EditMapTileData()
                {
                    ID          = nativeResult[i].ID,
                    NaviMask    = naviMask,
                    LinkMask    = 0, //LinkTiles(map);에서 처리 예정
                    RenderIndex = renderIndex,
                    LayerMask   = nativeRenderLayer[i]
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
            if (AssetDatabase.IsValidFolder(fullNaviPath))
            {
                AssetDatabase.DeleteAsset(fullNaviPath);
            }

            if (!System.IO.Directory.Exists(fullNaviPath))
            {
                System.IO.Directory.CreateDirectory(fullNaviPath);
            }

            AssetDatabase.Refresh();

            // 1. 개별 MapGridData 바이너리 파일 배포 루프
            foreach (KeyValuePair<int, EditMapGridData> grid in map)
            {
                MapGridData mapGridData = new MapGridData()
                {
                    Key = grid.Key,
                    NaviTileDict = grid.Value.ParseData(),
                    layerMeshAssets = grid.Value.LayerMeshAssets
                };

                Kompile.Asset.Editor.Provider.EditAssetProvider.WriteBinaryFile<MapGridData>(
                    data: mapGridData,
                    relativePath: MAP_NAVI_DATA_PATH,
                    fileName: $"MapNavi_{mapGridData.Key}",
                    addressableGroup: "MapNavi",
                    addressableLabel: "MapNavi"
                );
            }

            // =================================================================
            // ★ [추가 포인트] MapRegistryData (화이트 매니페스트) 저장 및 에셋 등록
            // =================================================================
            
            // 2. 이번 베이크 씬에서 유효성이 확정된 모든 고유 GridKey 수집
            List<int> validKeys = new List<int>(map.Keys);
            
            // 3. [중요] 런타임 MapRepoProvider의 이진 검색(BinarySearch) 성능 최적화를 위해 무조건 오름차순 정렬
            validKeys.Sort(); 

            MapRegistryData registryData = new MapRegistryData()
            {
                BakedGridKeys = validKeys.ToArray()
            };

            // 4. 단일 화이트리스트 파일 배포 및 Addressables 자동 생성
            Kompile.Asset.Editor.Provider.EditAssetProvider.WriteBinaryFile<MapRegistryData>(
                data: registryData,
                relativePath: MAP_NAVI_DATA_PATH,
                fileName: "MapRegistry", // 에셋 주소 및 파일명: "MapRegistry"
                addressableGroup: "MapNavi", // 관리 편의성을 위해 내비게이션 그룹에 함께 편입
                addressableLabel: "MapRegistry"
            );

            // =================================================================

            Debug.Log($"End Bake (length: {tiles.Length})");
            System.GC.Collect();
        }

        private void LinkTiles(ConcurrentDictionary<int, EditMapGridData> map)
        {
            List<EditMapTileData> allTiles = new List<EditMapTileData>();
            foreach (EditMapGridData grid in map.Values)
            {
                foreach (EditMapTileData tile in grid.Data.Values)
                {
                    allTiles.Add(tile);
                }
            }

            int count = allTiles.Count;
            if (0 == count)
            {
                return;
            }

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

                if (true == map.TryGetValue(gKey, out EditMapGridData gridData))
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
            if (null == tiles || 0 == tiles.Length)
            {
                return;
            }

            if (true == AssetDatabase.IsValidFolder(SAVE_PATH_ROOT))
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

            var accumulators = new Dictionary<EditBakeGroupKey, EditBakeAccumulator>();
            int totalTiles = tiles.Length;
            float totalTiles_recip = 1 / totalTiles;
            bool userCancelled = false;

            try
            {
                int start = 0;
                List<int> batchIndices = new List<int>();
                while (start < totalTiles)
                {
                    if (true == EditorUtility.DisplayCancelableProgressBar("Bake Map - Combining Meshes",
                            $"Processing {start}/{totalTiles}",
                            (float)start * totalTiles_recip))
                    {
                        userCancelled = true;
                        break;
                    }

                    batchIndices.Clear();
                    int currentBatchVertexCount = 0;
                    int idx = start;

                    while (idx < totalTiles
                           && batchIndices.Count < BATCH_TILE_LIMIT)
                    {
                        EditMapTileComponent tile = tiles[idx];
                        int vertexCount = 0;
                        
                        // [수정 포인트] TryGetSharedMesh 대신 MeshFilter 직접 접근으로 변경
                        if (tile.MeshFilter != null && tile.MeshFilter.sharedMesh != null)
                        {
                            vertexCount = tile.MeshFilter.sharedMesh.vertexCount;
                        }

                        if (BATCH_VERTEX_TARGET < currentBatchVertexCount + vertexCount
                            && 0 < batchIndices.Count)
                        {
                            break;
                        }

                        batchIndices.Add(idx);
                        currentBatchVertexCount += vertexCount;
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
                EditBakeGroupKey key = kv.Key;
                EditBakeAccumulator accm = kv.Value;

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
            List<int> indices, Dictionary<EditBakeGroupKey, EditBakeAccumulator> accums)
        {
            foreach (int i in indices)
            {
                EditMapTileComponent tile = tilesInGrid[i];
                
                // [수정 포인트] TryGetSharedMesh 대신 MeshFilter 직접 접근으로 변경
                if (!tile.MeshFilter || !tile.MeshFilter.sharedMesh)
                {
                    continue;
                }

                Mesh mesh = tile.MeshFilter.sharedMesh;
                int vc = mesh.vertexCount;
                if (vc == 0)
                {
                    continue;
                }

                int correctGridKey = gridKeys[i];

                string topAtlasName = (true == tile.TopAtlasTexture) ? tile.TopAtlasTexture.name : "None";
                string sideAtlasName = (true == tile.SideAtlasTexture) ? tile.SideAtlasTexture.name : "None";

                EditBakeGroupKey key = new EditBakeGroupKey
                {
                    RenderLayer = tile.RenderLayer,
                    GridKey = correctGridKey,
                    TopAtlas = topAtlasName,
                    SideAtlas = sideAtlasName,
                    TopTexRef = tile.TopAtlasTexture,
                    SideTexRef = tile.SideAtlasTexture
                };

                EditBakeChunkData chunkData = (0 < chunkPool.Count) ? chunkPool.Pop() : new EditBakeChunkData();
                chunkData.Instance = new CombineInstance { mesh = mesh, transform = tile.transform.localToWorldMatrix };
                chunkData.VertexCount = vc;
                chunkData.RenderLayer = tile.RenderLayer;
                chunkData.GridKey = correctGridKey;
                chunkData.TopTextureIndex = tile.TopTextureIndex;
                chunkData.SideTextureIndex = tile.SideTextureIndex;

                if (false == accums.TryGetValue(key, out EditBakeAccumulator acc))
                {
                    acc = (0 < accmPool.Count) ? accmPool.Pop() : new EditBakeAccumulator();
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

        private static void FlushAccumulatorPart(EditBakeContext ctx, EditBakeGroupKey key, EditBakeAccumulator acc)
        {
            if (0 == acc.Tiles.Count)
            {
                return;
            }

            List<CombineInstance> takeInstances = new List<CombineInstance>();
            List<Mesh> tempMeshes = new List<Mesh>();
            int takenVerts = 0;
            int tilesConsumed = 0;
            float uvStep = 1f / 8f;

            foreach (EditBakeChunkData chunk in acc.Tiles)
            {
                if (0 < takenVerts
                    && VERTEX_LIMIT < takenVerts + chunk.VertexCount)
                {
                    break;
                }

                Mesh tempMesh = Object.Instantiate(chunk.Instance.mesh);
                int vertexCount = tempMesh.vertexCount;
                Vector2[] uv2 = new Vector2[vertexCount];
                Vector2[] uv3 = new Vector2[vertexCount];

                int tLocal = chunk.TopTextureIndex % 64;
                Vector2 tOffset = new Vector2((tLocal % 8) * uvStep, 1.0f - ((tLocal / 8 + 1) * uvStep));

                int sLocal = chunk.SideTextureIndex % 64;
                Vector2 sOffset = new Vector2((sLocal % 8) * uvStep, 1.0f - ((sLocal / 8 + 1) * uvStep));

                for (int j = 0; j < vertexCount; j++)
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

                if (VERTEX_LIMIT < takenVerts)
                {
                    break;
                }
            }

            SaveMeshAsset(ctx, key, acc.PartIndex, takeInstances.ToArray());

            foreach (var tm in tempMeshes)
            {
                Object.DestroyImmediate(tm);
            }

            for (int i = 0; i < tilesConsumed; ++i)
            {
                var removed = acc.Tiles.Dequeue();
                acc.VertexSum -= removed.VertexCount;
                chunkPool.Push(removed);
            }

            ++acc.PartIndex;
        }

        private static void SaveMeshAsset(EditBakeContext ctx, EditBakeGroupKey key, int partIdx,
            CombineInstance[] instances)
        {
            // Material 에셋 자동 생성 및 등록
            string matName = $"Mat_{key.TopAtlas}_{key.SideAtlas}";
            string matPath = $"{SAVE_PATH_ROOT}/{matName}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (false == mat)
            {
                Shader shader = Shader.Find(SHADER_PATH);
                if (true == shader)
                {
                    mat = new Material(shader);
                    if (key.TopTexRef) mat.SetTexture("_TopAtlas", key.TopTexRef);
                    if (key.SideTexRef) mat.SetTexture("_SideAtlas", key.SideTexRef);

                    AssetDatabase.CreateAsset(mat, matPath);
                    ctx.CreatedAssets.Add((matPath, matName));
                }
                else
                {
                    Debug.LogWarning($"[Framework] '{SHADER_PATH}' 쉐이더를 찾을 수 없어 {matName} 생성을 건너뜁니다.");
                }
            }

            // 추가/수정된 핵심 부분: 머티리얼이 새로 생성되었든 기존에 있었든 무조건 텍스처 갱신
            if (mat)
            {
                if (key.TopTexRef) mat.SetTexture("_TopAtlas", key.TopTexRef);
                if (key.SideTexRef) mat.SetTexture("_SideAtlas", key.SideTexRef);

                // 변경 사항을 에셋에 강제로 저장
                EditorUtility.SetDirty(mat);
            }

            // 메쉬 저장 (기존 유지)
            string assetName =
                $"MapRender_{ctx.SceneIndex}_G{key.GridKey}_L{key.RenderLayer}_{key.TopAtlas}_{key.SideAtlas}_{partIdx}";
            string path = $"{SAVE_PATH_ROOT}/{assetName}.asset";

            Mesh combinedMesh = new Mesh();
            try
            {
                int totalVerts = 0;
                foreach (var ci in instances)
                {
                    totalVerts += ci.mesh.vertexCount;
                }

                if (VERTEX_LIMIT < totalVerts)
                {
                    combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }

                combinedMesh.CombineMeshes(instances, true, true);
                MeshUtility.Optimize(combinedMesh);

                if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
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
            if (!settings || 0 == ctx.CreatedAssets.Count)
            {
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(ctx.AddressableGroupName);
            if (!group)
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