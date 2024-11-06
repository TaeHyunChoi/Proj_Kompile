using UnityEngine;
using UnityEditor;

public class CubeMeshGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Cube Mesh with Origin at Top Vertex")]
    public static void GenerateAndSaveCubeMesh()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        /* 시계 방향으로 vertex 찍어야 정방향이다. */
        mesh.vertices = new Vector3[]
        {
            /*윗면0*/ new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, 0f),
            /*윗면1*/ new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 1f),
            /*앞면4*/ new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(1f, -1f, 0f),
            /*앞면5*/ new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
            /*뒷면6*/ new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 1f), new Vector3(0f, -1f, 1f),  
            /*뒷면7*/ new Vector3(1f, 0f, 1f), new Vector3(0f, -1f, 1f), new Vector3(1f, -1f, 1f), 
            /*왼쪽면8*/ new Vector3(0f,0f,0f), new Vector3(0f,-1f,1f), new Vector3(0f,0f,1f), 
            /*왼쪽면9*/ new Vector3(0f,0f,0f), new Vector3(0f,-1f,0f), new Vector3(0f,-1f,1f),
            /*오른쪽면10*/ new Vector3(1f,0f,0f), new Vector3(1f,0,1f), new Vector3(1f,-1f,1f),
            /*오른쪽면11*/ new Vector3(1f,0f,0f), new Vector3(1f,-1f,1f), new Vector3(1f,-1f,0f), 
            // /*아랫면2*/ new Vector3(0f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, -1f, 1f),
            // /*아랫면3*/ new Vector3(0f, -1f, 0f), new Vector3(1f, -1f, 1f), new Vector3(0f, -1f, 1f),
        };

        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new int[]
        {
            /*윗면*/ 
            0, 1, 2, 
            3, 4, 5,
            /*앞면*/ 
            6, 7, 8, 
            9, 10, 11,
            /*뒷면*/ 
            12, 13, 14, 
            15, 16, 17,
            /*왼쪽면*/ 
            18, 19, 20, 
            21, 22, 23,
            /*오른쪽면*/ 
            24, 25, 26, 
            27, 28, 29,
            /*아랫면*/ 
            30, 31, 32, 
            33, 34, 35
        };

        // 각 정점의 노멀 벡터 설정
        mesh.normals = new Vector3[]
        {
            Vector3.up, Vector3.up, Vector3.up,
            Vector3.up, Vector3.up, Vector3.up,
            Vector3.back, Vector3.back, Vector3.back,
            Vector3.back, Vector3.back, Vector3.back,
            Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.left, Vector3.left, Vector3.left,
            Vector3.left, Vector3.left, Vector3.left,
            Vector3.right, Vector3.right, Vector3.right,
            Vector3.right, Vector3.right, Vector3.right,
            // Vector3.down, Vector3.down, Vector3.down,
            // Vector3.down, Vector3.down, Vector3.down,
        };

        // UV 좌표 설정
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };

        // 에셋 저장 경로
        string assetPath = "Assets/CubeMesh_TopOrigin.asset";

        // 에셋으로 저장
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log("Cube Mesh asset created with origin as top vertex at: " + assetPath);
    }
}

/// <summary>
/// 재수정 요망.
/// </summary>
public class DiagonalCutCubeMeshGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Diagonal Cut Cube Mesh")]
    public static void GenerateAndSaveDiagonalCutCubeMesh()
    {
        Mesh mesh = new Mesh();

        // 사선으로 절반 잘린 정육면체의 정점 배열
        Vector3[] vertices = {
            new Vector3(0f, 0f, 0f),   // 원점 꼭지점
            new Vector3(1f, 0f, 0f),   // x축 1만큼 이동한 꼭지점
            new Vector3(1f, 0f, 1f),   // x축, z축 1만큼 이동한 꼭지점
            new Vector3(0f, 0f, 1f),   // z축 1만큼 이동한 꼭지점
            new Vector3(1f, 1f, 1f),   // 위쪽 대각선 꼭지점
            new Vector3(0f, 1f, 1f)    // 위쪽 대각선 꼭지점
        };

        // 각 면을 정의하는 삼각형 인덱스 배열
        int[] triangles = {
            0, 2, 1, 0, 3, 2,       // 아랫면
            0, 1, 4, 0, 4, 5,       // 옆면1
            1, 2, 4,                // 옆면2 - 대각선 잘린 면
            0, 5, 3,                // 옆면3 - 대각선 잘린 면
            3, 5, 4, 3, 4, 2        // 윗면
        };

        // UV 좌표 배열 설정 (텍스처 매핑 가능)
        Vector2[] uv = {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(1, 1), new Vector2(0, 1)
        };

        // Mesh의 속성 설정
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // 조명 처리를 위한 Normals 계산
        mesh.RecalculateNormals();

        // 에셋 저장 경로
        string assetPath = "Assets/DiagonalCutCubeMesh.asset";

        // Mesh를 에셋으로 저장
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log("Diagonal Cut Cube Mesh asset created at: " + assetPath);
    }
}