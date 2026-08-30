#if UNITY_EDITOR
namespace Kompile.Editor.Utility
{
    using Kompile.Editor.Entities;
    using Kompile.Data;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using static Kompile.Data.MapConsts;

    [InitializeOnLoad] // 에디터 로드 시 실행 보장
    public static class EditMapTileOperator
    {
        // [새로 추가] 컴포넌트의 신호(Undo 발생 등)를 감지하여 메쉬를 자동 갱신합니다.
        static EditMapTileOperator()
        {
            EditMapTileComponent.OnEditorDataChanged = (tile) =>
            {
                if (tile == null) return;

                // 유니티 내부 사이클 충돌을 방지하기 위해 한 프레임 뒤에 안전하게 실행
                EditorApplication.delayCall += () =>
                {
                    if (tile == null) return;
                    RefreshMesh(tile);
                    tile.UpdateVisual();
                    EditorUtility.SetDirty(tile);
                };
            };
        }
        
        public static void RefreshMesh(EditMapTileComponent tile, sbyte[] neighborHeights = null)
        {
            if (!tile.MeshFilter) return;
            var mesh = EditMapMeshUtil.GenerateMesh(tile.HeightData, neighborHeights);
            mesh.name = "Generated3DBlockMesh";
            
            if (tile.MeshFilter.sharedMesh && tile.MeshFilter.sharedMesh.name == "Generated3DBlockMesh")
                Object.DestroyImmediate(tile.MeshFilter.sharedMesh, true);
                
            tile.MeshFilter.sharedMesh = mesh;
            if (tile.TryGetComponent<MeshCollider>(out var mc)) mc.sharedMesh = mesh;
            
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

            MapTileHeightsData data = tile.HeightData;
            data.EnsureInitialized();
            
            // [핵심 해결] C# 구조체 내의 배열은 참조 타입입니다.
            // 기존 배열을 그대로 수정하면 Undo 시스템이 복구를 포기합니다.
            // Clone()을 통해 완전히 새로운 배열로 교체하여 참조를 끊어줍니다!
            sbyte[] newArray = (sbyte[])data.PointHeights.Clone();
            newArray[pointIndex] = (sbyte)Mathf.Clamp(newArray[pointIndex] + delta, -1, 8);
            data.PointHeights = newArray;

            // 수정된 데이터 덮어쓰기
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

            tile.UpdateVisual();
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
            return (tile.HeightData[index] == -1) ? 0f : (tile.HeightData[index] * EditMapMeshUtil.HeightStep);
        }
    }
}
#endif