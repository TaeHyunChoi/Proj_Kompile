using UnityEngine;
using UnityEditor;

public class CubeMeshGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Cube Mesh with Origin at Top Vertex")]
    public static void GenerateAndSaveCubeMesh()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        Vector3[] vertices = {
            new Vector3(0f, 0f, 0f),       // 원점 꼭지점 (윗면의 꼭지점 중 하나)
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, -1f, 0f),
            new Vector3(1f, -1f, 0f),
            new Vector3(1f, -1f, 1f),
            new Vector3(0f, -1f, 1f)
        };

        // 삼각형을 이루는 인덱스 설정
        int[] triangles = {
            0, 2, 1, 0, 3, 2, // 윗면
            4, 5, 6, 4, 6, 7, // 아랫면
            0, 1, 5, 0, 5, 4, // 앞면
            2, 3, 7, 2, 7, 6, // 뒷면
            0, 4, 7, 0, 7, 3, // 왼쪽면
            1, 2, 6, 1, 6, 5  // 오른쪽면
        };

        // UV 좌표 설정
        Vector2[] uv = {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        // 에셋 저장 경로
        string assetPath = "Assets/CubeMesh_TopOrigin.asset";

        // 에셋으로 저장
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log("Cube Mesh asset created with origin as top vertex at: " + assetPath);
    }
}

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