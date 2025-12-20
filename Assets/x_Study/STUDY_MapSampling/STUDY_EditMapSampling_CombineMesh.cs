#if UNITY_EDITOR
namespace Study.MapSampling
{
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;
    using Script.Data;

    public partial class STUDY_EditMapSampling
    {
        private readonly string ADDRESSABLE_GROUP_NAME = "MapRender";

        /// <summary> UV 계산용 Job (Batch 내에서 각 타입별로 작업) </summary>
        private struct TileInfo
        {
            public long key;            // 사용자 정의 key
            public int textureIndex;
            public int vertexOffset;    // Batch 평탄 배열 내에서의 시작 오프셋
            public int vertexCount;
            public int gridKey;
            public int renderLayer;
            public int sceneIndex;
            public int tileIndex;
        }

        private struct UVJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<TileInfo> TileInfos;
            [ReadOnly] public NativeArray<float3> Vertices;     // flattened for batch

            public NativeArray<float2> OutUVs;

            public float spriteSize;
            public int atlasWidth;
            public int atlasHeight;

            // TODO: 이거 설명 들어야 함
            public void Execute(int index)
            {
                TileInfo ti = TileInfos[index];
                if (0 == ti.vertexCount)
                {
                    return;
                }

                int cols = atlasWidth / (int)spriteSize;
                int col = ti.textureIndex % cols;
                int row = ti.textureIndex / cols;

                float uvWidth = spriteSize / (float)atlasWidth;
                float uvHeight = spriteSize / (float)atlasHeight;

                float uvX = col * uvWidth;
                float uvY = 1f - (row + 1) * uvHeight;

                int start = ti.vertexOffset;
                int count = ti.vertexCount;

                for (int i = 0; i < count; ++i)
                {
                    float3 v = Vertices[start + i];
                    float normalizedX = v.x;
                    float normalizedY = v.y;
                    OutUVs[start + i] = new float2(uvX + normalizedX * uvWidth, uvY + normalizedY * uvHeight);
                }
            }
        }

        /// <summary> 그룹(key)마다 유지되는 임시 누적기 (Batch 간 공유) </summary>
        private class GroupAccumulator
        {
            public List<CombineInstance> combineInstances = new List<CombineInstance>();
            public List<Vector2> combinedUVs = new List<Vector2>();
            public int vertexCount = 0;
            public int partIndex = 0;
        }

        private void CombineMapMeshes(ConcurrentDictionary<int, EditMapGridData> map, EditMapData[] tiles)
        {
            if (null == tiles || 0 == tiles.Length)
            {
                return;
            }

            // streaming parameter (실제 메모리에 따라 변경할 것)
            const int BATCH_TILE_LIMIT = 512;
            const int BATCH_VERTEX_TARGET = 20000;

            // 그룹별 누적기 (Batch 사이에 유지)
            var accumulators = new Dictionary<long, GroupAccumulator>();

            // 생성된 asset 수집 (마지막에 addressable 일괄 등록)
            List<(string path, string assetName)> createdAssets = new List<(string path, string assetName)>();

            int totalTiles = tiles.Length;
            bool userCancelled = false;

            try
            {
                string title = "Bake Map - Combining Meshes";
                string info;
                for (int start = 0; start < totalTiles;)
                {
                    // Progress Bar
                    float progress = (float)start / (float)totalTiles;
                    info = $"Processing tiles {start}/{totalTiles}";
                    if (true == EditorUtility.DisplayCancelableProgressBar(title, info, progress))
                    {
                        userCancelled = true;
                        Debug.LogWarning($"Bake cancelled by user. Finalizing current accumulators...");
                        break;
                    }

                    // Build a batch (tile indices)
                    int batchVertexSum = 0;
                    var batchIndices = new List<int>(Mathf.Min(BATCH_TILE_LIMIT, totalTiles - start));
                    int idx = start;

                    while (idx < totalTiles
                        && batchIndices.Count < BATCH_TILE_LIMIT)
                    {
                        EditMapData t = tiles[idx];
                        int vc = 0;
                        if (null != t
                            && null != t.MeshFilter
                            && null != t.MeshFilter.sharedMesh)
                        {
                            vc = t.MeshFilter.sharedMesh.vertexCount;
                        }

                        if (batchVertexSum + vc > BATCH_VERTEX_TARGET
                            && batchIndices.Count > 0)
                        {
                            // 하나의 타일이 BATCH_VERTEX_TARGET보다 클 경우, 
                            // 한계값은 넘더라도 '예외적으로' 이건 저장하고, 다음으로 넘어가겠다는 뜻
                            break;
                        }

                        batchIndices.Add(idx);
                        batchVertexSum += vc;
                        ++idx;
                    }

                    // Advance Start
                    start += batchIndices.Count;
                    if (0 == batchIndices.Count)
                    {
                        //배치 목표 정점 수를 넘어서서 타일 하나도 배치에 넣지 못한 경우,
                        //그래도 최소한 타일 하나는 처리되도록 강제로 배치에 포함시킨다.

                        if (idx < totalTiles)
                        {
                            batchIndices.Add(idx);
                            start = idx + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // --- Prepare batch flat arrays (main thread access to Mesh.vertices) ---
                    int batchCount = batchIndices.Count;
                    int batchTotalVerts = 0;

                    // 하나의 클래스로 묶는게 더 좋지 않나?...
                    var batchMeshes = new List<Mesh>(batchCount);
                    var batchTransforms = new List<Matrix4x4>(batchCount);
                    var batchTextureIndex = new List<int>(batchCount);
                    var batchGridKey = new List<int>(batchCount);
                    var batchRenderLayer = new List<int>(batchCount);
                    var batchVertexCounts = new List<int>(batchCount);

                    // First pass: gather meshes and vertex counts
                    for (int b = 0; b < batchCount; ++b)
                    {
                        int ti = batchIndices[b];
                        var tile = tiles[ti];
                        if (null == tile
                            || null == tile.MeshFilter
                            || null == tile.MeshFilter.sharedMesh)
                        {
                            batchMeshes.Add(null);
                            batchTransforms.Add(Matrix4x4.identity);
                            batchTextureIndex.Add(0);
                            batchGridKey.Add(0);
                            batchRenderLayer.Add(0);
                            batchVertexCounts.Add(0);
                            continue;
                        }

                        Mesh m = tile.MeshFilter.sharedMesh;
                        int vc = m.vertexCount;
                        batchMeshes.Add(m);
                        batchTransforms.Add(tile.transform.localToWorldMatrix);
                        batchTextureIndex.Add(tile.TextureIndex);
                        batchGridKey.Add(tile.GridKey);
                        batchRenderLayer.Add(tile.RenderLayer);
                        batchVertexCounts.Add(vc);
                        batchTotalVerts += vc;
                    }

                    if (0 == batchTotalVerts)
                    {
                        // nothing to do in this batch
                        continue;
                    }

                    Allocator alloc = Allocator.TempJob;
                    var flatVertices = new NativeArray<float3>(batchTotalVerts, alloc);
                    var tileInfos = new NativeArray<TileInfo>(batchCount, alloc);

                    // Fill flatVertices & tileInfos
                    int writeOffset = 0;
                    for (int b = 0; b < batchCount; ++b)
                    {
                        int vc = batchVertexCounts[b];
                        if (0 == vc)
                        {
                            tileInfos[b] = new TileInfo
                            {
                                key = 0,
                                textureIndex = 0,
                                vertexOffset = writeOffset,
                                vertexCount = 0,
                                gridKey = 0,
                                sceneIndex = 0,
                                tileIndex = batchIndices[b]
                            };

                            continue;
                        }

                        Mesh mesh = batchMeshes[b];
                        var verts = mesh.vertices;  // main-thread safe
                        for (int v = 0; v < vc; ++v)
                        {
                            var vv = verts[v];
                            flatVertices[writeOffset + v] = new float3(vv.x, vv.y, vv.z);
                        }

                        // 임의로 이런 key를 만들어서 구별하는거구나?
                        long composedKey = ((long)batchRenderLayer[b] << 32) | ((long)sceneIndex << 24) | ((long)batchGridKey[b]);
                        tileInfos[b] = new TileInfo
                        {
                            key = composedKey,
                            textureIndex = batchTextureIndex[b],
                            vertexOffset = writeOffset,
                            vertexCount = vc,
                            gridKey = batchGridKey[b],
                            renderLayer = batchRenderLayer[b],
                            sceneIndex = sceneIndex,
                            tileIndex = batchIndices[b]
                        };

                        writeOffset += vc;
                    }

                    // Prepare UV output
                    var flatUVs = new NativeArray<float2>(batchTotalVerts, alloc);

                    // Schedule UV Job for this batch
                    UVJob uvJob = new UVJob
                    {
                        TileInfos = tileInfos,
                        Vertices = flatVertices,
                        OutUVs = flatUVs,
                        spriteSize = 256f,
                        atlasWidth = 2048,
                        atlasHeight = 2048,
                    };

                    JobHandle uvHandle = uvJob.Schedule(tileInfos.Length, 16);
                    uvHandle.Complete();

                    // Apply batch results to accumulaotrs (main thread)
                    for (int b = 0; b < tileInfos.Length; ++b)
                    {
                        TileInfo ti = tileInfos[b];
                        if (0 == ti.vertexCount)
                        {
                            continue;
                        }

                        long key = ti.key;
                        if (false == accumulators.TryGetValue(key, out GroupAccumulator acc))
                        {
                            acc = new GroupAccumulator();
                            accumulators[key] = acc;
                        }

                        // Append CombineInstance
                        CombineInstance ci = new CombineInstance
                        {
                            mesh = batchMeshes[b],
                            transform = batchTransforms[b]
                        };
                        acc.combineInstances.Add(ci);

                        // Append UVs for this tile
                        int startUV = ti.vertexOffset;
                        for (int u = 0; u < ti.vertexCount; ++u)
                        {
                            float2 uv = flatUVs[startUV + u];
                            acc.combinedUVs.Add(new Vector2(uv.x, uv.y));
                        }
                        acc.vertexCount += ti.vertexCount;


                        //Flush if exceed VERTEX_LIMIT
                        while (VERTEX_LIMIT < acc.vertexCount) // 초과분이 사라질 때까지 반복
                        {
                            // 정확하게 이게 무슨 말일까?
                            // Build mesh from first subset of instances whose total vertices <= VERTEX_LIMIT
                            // Because combineInstances are per-tile, we need to take leading tiles until vertex sum <= VERTEX_LIMIT
                            var takeInstances = new List<CombineInstance>();
                            var takeUVs = new List<Vector2>();
                            int takenVerts = 0;

                            // We`ll remove from the front (older items)
                            // To map vertex counts per instance, we must compute per-instance vertex counts using combinedUVs (we don`t store per-instance vc seperately here).
                            // Simpler: because we appended UVs per tile in exact sequence, and combineInstances per tile, we can iterate combineInstances and track vertex counts by counting vertices using mesh.vertexCount
                            int instanceIndex = 0;
                            int uvCursor = 0;
                            while (instanceIndex < acc.combineInstances.Count
                                && takenVerts + acc.combineInstances[instanceIndex].mesh.vertexCount <= VERTEX_LIMIT)
                            {
                                var inst = acc.combineInstances[instanceIndex];
                                takeInstances.Add(inst);
                                int thisVC = inst.mesh.vertexCount;

                                // copy thisVC uvs from acc.combinedUVs starting at uvCursor
                                for (int k = 0; k < thisVC; ++k)
                                {
                                    takeUVs.Add(acc.combinedUVs[uvCursor + k]);
                                }

                                uvCursor += thisVC;
                                takenVerts += thisVC;
                                ++instanceIndex;
                            }

                            if (0 == takeInstances.Count)
                            {
                                // Single instance exceed VERTEX_LIMIT (rare). Force Split by taking first instance anyway.
                                var inst = acc.combineInstances[0];
                                takeInstances.Add(inst);
                                int thisVC = inst.mesh.vertexCount;
                                for (int k = 0; k < thisVC; ++k)
                                {
                                    takeUVs.Add(acc.combinedUVs[k]);
                                }
                                takenVerts = thisVC;
                                instanceIndex = 1;
                                uvCursor = thisVC;
                            }

                            // Create combined mesh for this part
                            Mesh combinedMesh = new Mesh();
                            combinedMesh.CombineMeshes(takeInstances.ToArray(), true, true);
                            combinedMesh.uv = takeUVs.ToArray();

                            if (takeUVs.Count != combinedMesh.vertexCount)
                            {
                                Debug.LogError($"[Streamed]UV/Vertex mismatch! Vertices: {combinedMesh.vertexCount}, UVs: {takeUVs.Count}");
                            }

                            // Save Asset
                            int layer = (int)(key >> 32);
                            int gk = (int)(key & 0xFFFFFF);
                            string assetName = $"MapRender_{sceneIndex}_G{gk}_L{layer}_{acc.partIndex}";
                            string path = $"Assets/Rcs/MapRender/{assetName}.asset";

                            if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
                            {
                                AssetDatabase.DeleteAsset(path);
                            }

                            Mesh meshToSave = Instantiate(combinedMesh);
                            MeshUtility.Optimize(meshToSave);
                            AssetDatabase.CreateAsset(meshToSave, path);

                            createdAssets.Add((path, assetName));
                            DestroyImmediate(combinedMesh);

                            // Remove consumed instances and uvs from accumulator
                            acc.combineInstances.RemoveRange(0, instanceIndex);
                            acc.combinedUVs.RemoveRange(0, instanceIndex);
                            acc.vertexCount -= takenVerts;
                            ++acc.partIndex;
                        }
                    }

                    // Dispose job native arrays for this batch
                    flatVertices.Dispose();
                    tileInfos.Dispose();
                    flatUVs.Dispose();

                    // Check cancellation after batch completion
                    if (true == userCancelled)
                    {
                        break;
                    }
                } // end for batches
            }
            finally
            {
                // Always clear Progress bar
                EditorUtility.ClearProgressBar();
            }

            // Finalize any remaining accumulators (flush)
            foreach (var kv in accumulators)
            {
                long key = kv.Key;
                var acc = kv.Value;
                if (0 == acc.combineInstances.Count)
                {
                    continue;
                }

                // May need multiple parts if still larger than VERTEX_LIMIT
                while (0 < acc.combineInstances.Count)
                {
                    // Build as many leading instances within VERTEX_LIMIT
                    var takeInstances = new List<CombineInstance>();
                    var takeUVs = new List<Vector2>();
                    int takenVerts = 0;

                    int instanceIndex = 0;
                    int uvCursor = 0;
                    while (instanceIndex < acc.combineInstances.Count
                        && takenVerts + acc.combineInstances[instanceIndex].mesh.vertexCount <= VERTEX_LIMIT)
                    {
                        var inst = acc.combineInstances[instanceIndex];
                        takeInstances.Add(inst);
                        int thisVC = inst.mesh.vertexCount;
                        for (int k = 0; k < thisVC; ++k)
                        {
                            takeUVs.Add(acc.combinedUVs[uvCursor + k]);
                        }

                        uvCursor += thisVC;
                        takenVerts += thisVC;
                        ++instanceIndex;
                    }

                    if (0 == takeInstances.Count)
                    {
                        // force take one
                        var inst = acc.combineInstances[0];
                        takeInstances.Add(inst);
                        int thisVC = inst.mesh.vertexCount;

                        for (int k = 0; k < thisVC; ++k)
                        {
                            takeUVs.Add(acc.combinedUVs[k]);
                        }

                        Mesh combinedMesh = new Mesh();
                        combinedMesh.CombineMeshes(takeInstances.ToArray(), true, true);
                        combinedMesh.uv = takeUVs.ToArray();

                        if (takeUVs.Count != combinedMesh.vertexCount)
                        {
                            Debug.LogError($"[Finalized] UV/Vertex mismatch! Vertices: {combinedMesh.vertexCount}, UVs: {takeUVs.Count}");
                        }

                        int layer = (int)(key >> 32);
                        int gk = (int)(key & 0xFFFFFF);
                        string assetName = $"MapRender_{sceneIndex}_G{gk}_L{layer}_{acc.partIndex}";
                        string path = $"Assets/Rcs/MapRender/{assetName}.asset";

                        if (null != AssetDatabase.LoadAssetAtPath<Mesh>(path))
                        {
                            AssetDatabase.DeleteAsset(path);
                        }

                        Mesh meshToSave = Instantiate(combinedMesh);
                        MeshUtility.Optimize(meshToSave);
                        AssetDatabase.CreateAsset(meshToSave, path);

                        createdAssets.Add((path, assetName));

                        DestroyImmediate(combinedMesh);

                        // Remove consumed
                        acc.combineInstances.RemoveRange(0, instanceIndex);
                        acc.combinedUVs.RemoveRange(0, instanceIndex);
                    }
                }
            }

            // Regist Addressables
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (null == settings)
            {
                Debug.LogError("AddressableAssetSettings not found. Created mesh assets saved but not registered to Addressables.");
            }
            else
            {
                var group = settings.FindGroup(ADDRESSABLE_GROUP_NAME);
                if (null == group)
                {
                    Debug.LogError($"AddressableAssetSettings not found. Created mesh assets saved but not registered to Addressables.");
                }
                else
                {
                    List<AddressableAssetEntry> createdEntries = new List<AddressableAssetEntry>(createdAssets.Count);
                    foreach (var entry in createdAssets)
                    {
                        string path = entry.path;
                        string assetName = entry.assetName;

                        string guid = AssetDatabase.AssetPathToGUID(path);
                        if (true == string.IsNullOrEmpty(guid))
                        {
                            Debug.LogWarning($"Faild to find GUID for path '{path}'. Skipping Addressable registration for this asset.");
                            continue;
                        }

                        var addEntry = settings.CreateOrMoveEntry(guid, group, false, false);
                        addEntry.SetAddress(assetName);
                        createdEntries.Add(addEntry);
                    }

                    if (0 < createdEntries.Count)
                    {
                        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, createdEntries.ToArray(), true);
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            AssetDatabase.Refresh();

            if (true == userCancelled)
            {
                Debug.LogWarning($"Bake was cancelled by user. Some assets were created; Pipeline finished early;");
            }
            else
            {
                Debug.Log($"Bake Completed Succesfully.");
            }
        }
    }
}
#endif