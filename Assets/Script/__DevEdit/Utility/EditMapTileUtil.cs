#if UNITY_EDITOR
namespace Kompile.Editor.Utility
{
    using UnityEngine;
    using Editor.Entities;

    /// <summary> 타일 머티리얼 및 쉐이더 프로퍼티 블록 계산을 전담 </summary>
    public class EditMapTileUtil
    {
        private const float UV_STEP = 1f / 8f;
        private static readonly Vector2 UV_SCALE = new Vector2(UV_STEP, UV_STEP);
        private static MaterialPropertyBlock _sharedPropertyBlock;

        private static MaterialPropertyBlock SharedBlock
        {
            get
            {
                if (null == _sharedPropertyBlock)
                {
                    _sharedPropertyBlock = new MaterialPropertyBlock();
                }

                return _sharedPropertyBlock;
            }
        }

        public static void UpdateMaterialProperties(EditMapTileComponent tile, bool isVisualDimmed = false)
        {
            if (!tile)
            {
                return;
            }

            MeshRenderer renderer = tile.MeshRenderer;
            if (!renderer)
            {
                return;
            }

            MaterialPropertyBlock block = SharedBlock;
            if (tile.MeshRenderer.HasPropertyBlock())
            {
                tile.MeshRenderer.GetPropertyBlock(block);
            }
            else
            {
                block.Clear();
            }

            // UV Offset 계산
            float tx = tile.TopTextureIndex % 8 * UV_STEP;
            float ty = 1f - ((tile.TopTextureIndex / 8) + 1) * UV_STEP;
            Vector2 topOffset = new Vector2(tx, ty);

            float sx = (tile.SideTextureIndex % 8) * UV_STEP;
            float sy = ((tile.SideTextureIndex / 8) + 1) * UV_STEP;
            Vector2 sideOffset = new Vector2(sx, sy);

            block.SetVector("_TopUVOffset", topOffset);
            block.SetVector("_TopUVScale", UV_SCALE);
            block.SetVector("_SideUVOffset", sideOffset);
            block.SetVector("_SideUVScale", UV_SCALE);
            block.SetFloat("_IsBaked", 0f);

            if (tile.TopAtlasTexture)
            {
                block.SetTexture("_TopAtlas", tile.TopAtlasTexture);
                block.SetTexture("_MainTex", tile.TopAtlasTexture);
                block.SetTexture("_BaseMap", tile.TopAtlasTexture);
            }
            if (tile.SideAtlasTexture)
            {
                block.SetTexture("_SideAtlas", tile.SideAtlasTexture);
            }

            Color tint = isVisualDimmed ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.white;
            block.SetColor("_Color", tint);

            renderer.SetPropertyBlock(block);
        }
    }
}

#endif