#if UNITY_EDITOR
namespace Study.MapSampling
{
    using Script.Data;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;
    using UnityEngine.Rendering;

    public partial class STUDY_EditMapSampling
    {
        private const int VERTEX_LIMIT = 65535;
        private const int BATCH_TILE_LIMIT = 512;
        private const int BATCH_VERTEX_TARGET = 200000;
        private const float SPRITE_SIZE = 256f;
        private const int ATLAS_WIDTH = 2048;
        private const int ATLAS_HEIGHT = 2048;
        private const string SAVE_PATH_ROOT = "Assets/Rcs/MapRender";

        // 매개변수를 줄이기 위한 컨텍스트 구조체
        private class BakeContext
        {
            public int SceneIndex;
            public ConcurrentDictionary<int, EditMapGridData> Map;
            public List<(string path, string assetName)> CreatedAssets;
            public string AddressableGroupName;

            public BakeContext(int sceneIndex, ConcurrentDictionary<int, EditMapGridData> map, string groupName)
            {
                SceneIndex = sceneIndex;
                Map = map;
                AddressableGroupName = groupName;
                CreatedAssets = new List<(string path, string assetName)>();
            }
        }

        // 그룹 구분을 위한 키 구조체 (가독성 향상)
        private struct GroupKey
        {
            public readonly int RenderLayer;
            public readonly int GridKey;

            public GroupKey(int layer, int grid)
            {
                RenderLayer = layer;
                GridKey = grid;
            }

            public override int GetHashCode() => (RenderLayer, GridKey).GetHashCode();
            public override bool Equals(object obj) => obj is GroupKey other && other.RenderLayer == RenderLayer && other.GridKey == GridKey;
        }

        private class TileChunk
        {
            public CombineInstance Instance;
            public Vector2[] UVs;
            public int VertexCount;
            public int GridKey;
            public int RenderLayer;
        }

        private class GroupAccumulator
        {
            public Queue<TileChunk> Tiles = new Queue<TileChunk>();
            public int VertexSum = 0;
            public int PartIndex = 0;
        }

        public static void CombineAndRegister(ConcurrentDictionary<int, EditMapGridData> map, EditMapData[] tiles, int sceneIndex, string addressableGroupName)
        {
            if (tiles == null || tiles.Length == 0)
            {
                Debug.LogWarning("No tiles to process.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SAVE_PATH_ROOT))
            {
                System.IO.Directory.CreateDirectory(SAVE_PATH_ROOT);
            }

            var context = new BakeContext(sceneIndex, map, addressableGroupName);
            var accumulators = new Dictionary<GroupKey, GroupAccumulator>();
            int totalTiles = tiles.Length;
            bool userCancelled = false;

            try
            {
                string title = "Bake Map - Combining Meshes";
                for (int start = 0; start < totalTiles;)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(title, $"Processing {start}/{totalTiles}", (float)start / totalTiles))
                    {
                        userCancelled = true;
                        break;
                    }

                    // 배치 윈도우 계산
                    int currentBatchVertex = 0;
                    var batchIndices = new List<int>();
                    int idx = start;

                    while (idx < totalTiles && batchIndices.Count < BATCH_TILE_LIMIT)
                    {
                        var t = tiles[idx];
                        int vc = (t?.MeshFilter?.sharedMesh != null) ? t.MeshFilter.sharedMesh.vertexCount : 0;

                        if (currentBatchVertex + vc > BATCH_VERTEX_TARGET && batchIndices.Count > 0) break;

                        batchIndices.Add(idx);
                        currentBatchVertex += vc;
                        idx++;
                    }

                    start += batchIndices.Count;
                    ProcessBatch(context, tiles, batchIndices, accumulators);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // 남은 데이터 Flush
            foreach (var kv in accumulators)
            {
                while (kv.Value.Tiles.Count > 0)
                {
                    FlushAccumulatorPart(context, kv.Key, kv.Value);
                }
            }

            RegisterAddressables(context);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(userCancelled ? "Bake cancelled by user." : "Bake completed successfully.");
        }

        private static void ProcessBatch(BakeContext ctx, EditMapData[] allTiles, List<int> indices, Dictionary<GroupKey, GroupAccumulator> accums)
        {
            foreach (int i in indices)
            {
                var tile = allTiles[i];
                if (tile?.MeshFilter?.sharedMesh == null) continue;

                Mesh mesh = tile.MeshFilter.sharedMesh;
                int vc = mesh.vertexCount;
                if (vc == 0) continue;

                var chunk = new TileChunk
                {
                    Instance = new CombineInstance { mesh = mesh, transform = tile.transform.localToWorldMatrix },
                    UVs = CalculateAtlasUVs(mesh, tile.TextureIndex),
                    VertexCount = vc,
                    GridKey = tile.GridKey,
                    RenderLayer = tile.RenderLayer
                };

                var key = new GroupKey(tile.RenderLayer, tile.GridKey);
                if (!accums.TryGetValue(key, out var acc))
                {
                    acc = new GroupAccumulator();
                    accums[key] = acc;
                }

                acc.Tiles.Enqueue(chunk);
                acc.VertexSum += vc;

                while (acc.VertexSum > VERTEX_LIMIT)
                {
                    FlushAccumulatorPart(ctx, key, acc);
                }
            }
        }

        private static void FlushAccumulatorPart(BakeContext ctx, GroupKey key, GroupAccumulator acc)
        {
            if (acc.Tiles.Count == 0) return;

            var takeInstances = new List<CombineInstance>();
            var takeUVs = new List<Vector2>();
            int takenVerts = 0;
            int tilesConsumed = 0;

            // 단일 타일이 제한을 넘는 경우 강제 1개 처리, 아니면 제한 내에서 최대한 수집
            foreach (var chunk in acc.Tiles)
            {
                if (takenVerts > 0 && takenVerts + chunk.VertexCount > VERTEX_LIMIT) break;

                takeInstances.Add(chunk.Instance);
                takeUVs.AddRange(chunk.UVs);
                takenVerts += chunk.VertexCount;
                tilesConsumed++;

                if (takenVerts > VERTEX_LIMIT) break; // 단일 거대 타일 대응
            }

            SaveMeshAsset(ctx, key, acc.PartIndex, takeInstances.ToArray(), takeUVs.ToArray());

            for (int i = 0; i < tilesConsumed; i++)
            {
                var removed = acc.Tiles.Dequeue();
                acc.VertexSum -= removed.VertexCount;
            }
            acc.PartIndex++;
        }

        private static void SaveMeshAsset(BakeContext ctx, GroupKey key, int partIdx, CombineInstance[] instances, Vector2[] uvs)
        {
            string assetName = $"MapRender_{ctx.SceneIndex}_G{key.GridKey}_L{key.RenderLayer}_{partIdx}";
            string path = $"{SAVE_PATH_ROOT}/{assetName}.asset";

            Mesh combinedMesh = new Mesh();
            try
            {
                if (uvs.Length > 65535) combinedMesh.indexFormat = IndexFormat.UInt32;

                combinedMesh.CombineMeshes(instances, true, true);
                combinedMesh.uv = uvs; // CombineMeshes 이후에 UV를 덮어씌움

                MeshUtility.Optimize(combinedMesh);

                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(combinedMesh, path);

                // Map Grid Data 업데이트
                if (ctx.Map != null)
                {
                    var gridData = ctx.Map.GetOrAdd(key.GridKey, k => new EditMapGridData(k));
                    gridData.AddAssetFile(assetName);
                    gridData.AddMeshAsset(key.RenderLayer, assetName);
                }

                ctx.CreatedAssets.Add((path, assetName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save mesh {assetName}: {ex.Message}");
            }
        }

        private static void RegisterAddressables(BakeContext ctx)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || ctx.CreatedAssets.Count == 0) return;

            var group = settings.FindGroup(ctx.AddressableGroupName);
            if (group == null) return;

            var entries = new List<AddressableAssetEntry>();
            foreach (var item in ctx.CreatedAssets)
            {
                string guid = AssetDatabase.AssetPathToGUID(item.path);
                if (string.IsNullOrEmpty(guid)) continue;

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.SetAddress(item.assetName);
                entries.Add(entry);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries.ToArray(), true);
        }

        private static Vector2[] CalculateAtlasUVs(Mesh mesh, int textureIndex)
        {
            Vector3[] verts = mesh.vertices;
            Vector2[] uvs = new Vector2[verts.Length];

            int atlasCols = ATLAS_WIDTH / (int)SPRITE_SIZE;
            float uvW = SPRITE_SIZE / ATLAS_WIDTH;
            float uvH = SPRITE_SIZE / ATLAS_HEIGHT;

            float baseX = (textureIndex % atlasCols) * uvW;
            float baseY = 1f - ((textureIndex / atlasCols) + 1) * uvH;

            for (int i = 0; i < verts.Length; i++)
            {
                uvs[i] = new Vector2(baseX + verts[i].x * uvW, baseY + verts[i].y * uvH);
            }
            return uvs;
        }
    }
}
#endif