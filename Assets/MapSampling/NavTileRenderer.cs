using UnityEngine;

public class NavTileRenderer : MonoBehaviour
{
    private MeshFilter   meshFilter;
    private MeshRenderer meshRenderer;

    [SerializeField] int columnIndex = 0; // 텍스처의 열 인덱스
    [SerializeField] int rowIndex = 0;    // 텍스처의 행 인덱스
    [SerializeField] int spriteWidth = 128;
    [SerializeField] int spriteHeight = 128;

    private void Awake()
    {
        meshFilter   = transform.GetComponent<MeshFilter>();
        meshRenderer = transform.GetComponent<MeshRenderer>();
    }
    private void Start()
    {
        Texture texture = meshRenderer.material.mainTexture;
        int textureWidth  = texture.width;
        int textureHeight = texture.height;

        // UV 좌표 계산
        float uMin = columnIndex * (spriteWidth / (float)textureWidth);
        float uMax = (columnIndex + 1) * (spriteWidth / (float)textureWidth);
        float vMin = 1.0f - (rowIndex + 1) * (spriteHeight / (float)textureHeight);
        float vMax = 1.0f - rowIndex * (spriteHeight / (float)textureHeight);

        Mesh mesh = meshFilter.mesh;

        var uvs = mesh.uv;
        var vertices = mesh.vertices;

        for (int i = 0; i < uvs.Length; i++)
        {
            float u = Mathf.Lerp(uMin, uMax, vertices[i].x); // X축 기준
            float v = Mathf.Lerp(vMin, vMax, vertices[i].y); // Y축 기준
            uvs[i] = new Vector2(u, v);
        }

        mesh.uv = uvs;

        //mesh.uv = new Vector2[]
        //{
        //    new Vector2(uMin, vMin),
        //    new Vector2(uMax, vMin),
        //    new Vector2(uMax, vMax),
        //    new Vector2(uMin, vMax)
        //};
    }
}
