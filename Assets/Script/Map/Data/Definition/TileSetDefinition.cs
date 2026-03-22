namespace Script.Map.Data
{
    using Script.Index;
    using UnityEngine;

    /// <summary>
    /// [Framework] Data (Definition): 윗면(Top)과 옆면(Side) 텍스처의 조합을 정의하는 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTileSet", menuName = "Kompile/Map/Tile Set Definition")]
    public class TileSetDefinition : ScriptableObject
    {
        [Header("Texture Binding")]
        [Tooltip("타일의 윗면(잔디, 눈 등)에 적용될 텍스처입니다.")]
        public MapTextureType topTexture = MapTextureType.map_w;

        [Tooltip("타일의 옆면(흙, 바위 등)에 적용될 텍스처입니다.")]
        public MapTextureType sideTexture = MapTextureType.map_g;

        // 💡 나으리, 훗날 아래와 같은 데이터를 추가하여 확장하실 수 있습니다.
        // public AudioClip footstepSound;
        // public float movementFriction = 1.0f;
    }
}