#if UNITY_EDITOR
namespace Kompile.Map.Editor.Provider
{
    using Kompile.Map.Entity;
    using Kompile.Map.Data;
    using Kompile.Map.Editor.Utility;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using static Kompile.Map.Data.MapConsts;

    public static class EditMapTileOperator
    {
        public static void RefreshMesh(EditMapTileComponent tile, sbyte[] neighborHeights = null)
        {
            if (!tile.MeshFilter) return;
            Mesh newMesh = EditMapMeshUtil.GenerateMesh(tile.HeightData, neighborHeights);
            newMesh.name = "Generated3DBlockMesh";
            if (tile.MeshFilter.sharedMesh && tile.MeshFilter.sharedMesh.name == "Generated3DBlockMesh")
                Object.DestroyImmediate(tile.MeshFilter.sharedMesh, true);
            tile.MeshFilter.sharedMesh = newMesh;
            if (tile.TryGetComponent<MeshCollider>(out var mc)) mc.sharedMesh = newMesh;
            EditorUtility.SetDirty(tile);
        }

        public static void UpdateHeightMask(EditMapTileComponent tile)
        {
            ulong mask = 0;
            var data = tile.HeightData;
            for (int i = 0; i < 13; i++)
            {
                int h = data[i];
                ulong flag = (h == -1) ? HEIGHT_MASK : (ulong)h;
                mask |= (flag & HEIGHT_MASK) << (i * HEIGHT_BITS);
            }
            tile.SetHeightMask(mask);
            EditorUtility.SetDirty(tile);
        }

        public static void ModifyHeightIndex(EditMapTileComponent tile, int pointIndex, int delta)
        {
            Undo.RecordObject(tile, "Modify Height");
            
            // [해결] 리플렉션 없이 Setter 사용
            MapTileHeightsData data = tile.HeightData;
            data[pointIndex] = (sbyte)Mathf.Clamp(data[pointIndex] + delta, -1, 8);
            tile.HeightData = data; 

            UpdateHeightMask(tile);
            RefreshMesh(tile);
        }

        public static void ApplyTextures(EditMapTileComponent tile, int tIdx, Texture2D tAtlas, int sIdx, Texture2D sAtlas)
        {
            Undo.RecordObject(tile, "Apply Textures");
            var type = typeof(EditMapTileComponent);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("topTextureIndex", flags).SetValue(tile, tIdx);
            type.GetField("sideTextureIndex", flags).SetValue(tile, sIdx);
            if (tAtlas) type.GetField("topAtlasTexture", flags).SetValue(tile, tAtlas);
            if (sAtlas) type.GetField("sideAtlasTexture", flags).SetValue(tile, sAtlas);

            tile.UpdateMaterialProperties();
            EditorUtility.SetDirty(tile);
        }

        public static void OptimizeSides(EditMapTileComponent tile, Dictionary<Vector2Int, EditMapTileComponent> tileMap)
        {
            Vector3 pos = tile.transform.position;
            Vector2Int gPos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));
            sbyte[] nh = new sbyte[16];
            var dirs = new (Vector2Int d, int v1, int v2, int v3)[] {
                (Vector2Int.down, 10, 11, 12), (Vector2Int.right, 0, 5, 10),
                (Vector2Int.up, 2, 1, 0), (Vector2Int.left, 12, 7, 2)
            };
            for (int i = 0; i < 4; i++)
            {
                if (tileMap.TryGetValue(gPos + dirs[i].d, out var n))
                {
                    nh[i * 4] = n.HeightData[dirs[i].v1]; nh[i * 4 + 1] = n.HeightData[dirs[i].v2];
                    nh[i * 4 + 2] = n.HeightData[dirs[i].v2]; nh[i * 4 + 3] = n.HeightData[dirs[i].v3];
                }
                else { for (int j = 0; j < 4; j++) nh[i * 4 + j] = -1; }
            }
            RefreshMesh(tile, nh);
        }

        public static float GetPointLocalY(EditMapTileComponent tile, int index)
        {
            return (tile.HeightData[index] == -1) ? 0f : (tile.HeightData[index] * 0.25f);
        }
    }
}
#endif