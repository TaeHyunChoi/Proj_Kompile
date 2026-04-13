namespace Kompile.Map.Data
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "NewTileSet", menuName = "Kompile/Map/Tile Set Definition")]
    public class TileSetDefinition : ScriptableObject
    {
        [Header("Texture Binding")]
        [Tooltip("타일의 윗면에 적용될 텍스처 전역 인덱스입니다.")]
        public int topTexture = 0;

        [Tooltip("타일의 옆면에 적용될 텍스처 전역 인덱스입니다.")]
        public int sideTexture = 0;
    }
}