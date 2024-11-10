using UnityEngine;
using UnityEditor;

public class MeshGenerator : MonoBehaviour
{
    private static void SaveMeshAsset(string assetName, Mesh mesh)
    {
        var path = "Assets/Rcs/MapNav/Mesh/" + assetName + ".asset";
        
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log("Mesh asset created: " + assetName);
    }

    //// Cube ////
    
    [MenuItem("Tools/NavTileMesh/Create/NavMesh_Cube_0")]
    public static void GenerateNavTileMesh_CubeDefault()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        /* 시계 방향으로 vertex 찍어야 정방향이다. */
        mesh.vertices = new []
        {
            /*윗면0*/ new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, 0f),
            /*윗면1*/ new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 1f),
            /*앞면4*/ new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(1f, -1f, 0f),
            /*앞면5*/ new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
            /*뒷면6*/ new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 1f), new Vector3(0f, -1f, 1f),
            /*뒷면7*/ new Vector3(1f, 0f, 1f), new Vector3(0f, -1f, 1f), new Vector3(1f, -1f, 1f),
            /*왼쪽면8*/ new Vector3(0f, 0f, 0f), new Vector3(0f, -1f, 1f), new Vector3(0f, 0f, 1f),
            /*왼쪽면9*/ new Vector3(0f, 0f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 1f),
            /*오른쪽면10*/ new Vector3(1f, 0f, 0f), new Vector3(1f, 0, 1f), new Vector3(1f, -1f, 1f),
            /*오른쪽면11*/ new Vector3(1f, 0f, 0f), new Vector3(1f, -1f, 1f), new Vector3(1f, -1f, 0f),
            // /*아랫면2*/ new Vector3(0f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, -1f, 1f),
            // /*아랫면3*/ new Vector3(0f, -1f, 0f), new Vector3(1f, -1f, 1f), new Vector3(0f, -1f, 1f),
        };

        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new []
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
            // 30, 31, 32,
            // 33, 34, 35
        };

        // 각 정점의 노멀 벡터 설정
        mesh.normals = new []
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
        mesh.uv = new []
        {
            // 윗면
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1),

            // 앞면
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1),

            // 뒷면
            new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0),
            new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0),

            // 왼쪽면
            new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0),
            new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0),

            // 오른쪽면
            new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0),
            new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0),

            // 아랫면
            // new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1),
            // new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1)
        };

        SaveMeshAsset("NavMesh_Cube_0", mesh);
    }

    //// Slope ////

    [MenuItem("Tools/NavTileMesh/Create/NavMesh_Slope_0")]
    public static void GenerateNavTileMesh_SlopeDefault()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        /* 시계 방향으로 vertex 찍어야 정방향이다. */
        mesh.vertices = new []
        {
            /*윗면*/
            new Vector3(0, 0, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1),
            new Vector3(0, 0, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 0),

            /*뒷면*/
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1),
            new Vector3(0, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1),

            /*왼쪽면*/
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1),

            /*오른쪽면*/
            new Vector3(1, 0, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1)
        };

        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new []
        {
            /*윗면*/
            0, 1, 2,
            3, 4, 5,
            /*뒷면*/
            6, 7, 8,
            9, 10, 11,
            /*왼쪽면*/
            12, 13, 14,
            /*오른쪽면*/
            15, 16, 17
        };

        // 각 정점의 노멀 벡터 설정
        mesh.normals = new []
        {
            /*윗면*/
            // 실제 빛반사를 하지 않으므로 탄젠트값 대신에 0,1로 구분하였다.
            Vector3.up, Vector3.up, Vector3.up,
            Vector3.up, Vector3.up, Vector3.up,

            /*뒷면*/
            Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.forward, Vector3.forward, Vector3.forward,

            /*왼쪽면*/
            Vector3.left, Vector3.left, Vector3.left,

            /*오른쪽면*/
            Vector3.right, Vector3.right, Vector3.right,
        };

        // UV 좌표 설정 : 실제 텍스쳐 값을 사용하지 않으므로 uv도 별도 설정 안함
        mesh.uv = new Vector2[]
        {
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
        };

        SaveMeshAsset("NavMesh_Slope_0", mesh);
    }

    [MenuItem("Tools/NavTileMesh/Create/NavMesh_SlopeHalf_0")]
    public static void GenerateNavTileMesh_SlopeHalf_1()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        /* 시계 방향으로 vertex 찍어야 정방향이다. */
        mesh.vertices = new []
        {
            /*윗면*/
            new Vector3(0, 0, 0), new Vector3(0, 0.5f, 1), new Vector3(1, 0.5f, 1),
            new Vector3(0, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 0),

            /*뒷면*/
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0.5f, 1),
            new Vector3(0, 0, 1), new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1),

            /*왼쪽면*/
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0.5f, 1),

            /*오른쪽면*/
            new Vector3(1, 0, 0), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 1)
        };

        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new []
        {
            /*윗면*/
            0, 1, 2,
            3, 4, 5,
            /*뒷면*/
            6, 7, 8,
            9, 10, 11,
            /*왼쪽면*/
            12, 13, 14,
            /*오른쪽면*/
            15, 16, 17
        };

        // 각 정점의 노멀 벡터 설정 : 실제 빛반사를 하지 않으므로 탄젠트값 대신에 0,1로 구분하였다.
        mesh.normals = new []
        {
            /*윗면*/
            Vector3.up, Vector3.up, Vector3.up,
            Vector3.up, Vector3.up, Vector3.up,

            /*뒷면*/
            Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.forward, Vector3.forward, Vector3.forward,

            /*왼쪽면*/
            Vector3.left, Vector3.left, Vector3.left,

            /*오른쪽면*/
            Vector3.right, Vector3.right, Vector3.right,
        };

        // UV 좌표 설정 : 실제 텍스쳐 값을 사용하지 않으므로 uv도 별도 설정 안함
        mesh.uv = new Vector2[]
        {
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero,
        };

        SaveMeshAsset("NavMesh_SlopeHalf_0", mesh);
    }

    [MenuItem("Tools/NavTileMesh/Create/NavMesh_SlopeHalf_1")]
    public static void GenerateNavTileMesh_SlopeHalf_2()
    {
        Mesh mesh = new Mesh();
    
        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        mesh.vertices = new []
        {
            // 윗면
            new Vector3(0, 0.5f, 0), new Vector3(1, 1, 1), new Vector3(1, 0.5f, 0),
            new Vector3(0, 0.5f, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1),
            // 앞면
            new Vector3(0, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 0, 0),
            new Vector3(0, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0),
            // 뒷면
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1),
            new Vector3(0, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
            // 왼쪽면
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1),
            new Vector3(0, 0, 0), new Vector3(0, 1, 1), new Vector3(0, 0.5f, 0),
            // 오른쪽 면
            new Vector3(1, 0, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1),
            new Vector3(1, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(1, 1, 1)
        };
    
        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new []
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
        };
    
        // 각 정점의 노멀 벡터 설정
        mesh.normals = new []
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
        };
    
        // UV 좌표 설정
        mesh.uv = new Vector2[]
        {
            Vector3.zero, Vector3.zero, Vector2.zero,
            Vector3.zero, Vector3.zero, Vector2.zero,
    
            Vector3.zero, Vector3.zero, Vector2.zero,
            Vector3.zero, Vector3.zero, Vector2.zero,
            
            Vector3.zero, Vector3.zero, Vector2.zero,
            Vector3.zero, Vector3.zero, Vector2.zero,
            
            Vector3.zero, Vector3.zero, Vector2.zero,
            Vector3.zero, Vector3.zero, Vector2.zero,
            
            Vector3.zero, Vector3.zero, Vector2.zero,
            Vector3.zero, Vector3.zero, Vector2.zero,
        };
    
        SaveMeshAsset("NavMesh_SlopeHalf_1", mesh);
    }
    
    [MenuItem("Tools/NavTileMesh/Create/NavMesh_SlopePartial_0")]
    public static void GenerateNavTileMesh_CubeSlope_0()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        /* 시계 방향으로 vertex 찍어야 정방향이다. */
        mesh.vertices = new[]
        {
            // 윗면
            new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 0f), new Vector3(0f, 0.5f, 1f), new Vector3(1f, 0f, 1f),
            //앞면
            new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(1f, -1f, 0f),
            new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
            //뒷면
            new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 1f), new Vector3(0f, -1f, 1f),
            new Vector3(1f, 0f, 1f), new Vector3(0f, -1f, 1f), new Vector3(1f, -1f, 1f),
            //왼쪽
            new Vector3(0f, 0f, 0f), new Vector3(0f, -1f, 1f), new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 1f),
            //오른쪽
            new Vector3(1f, 0f, 0f), new Vector3(1f, 0, 1f), new Vector3(1f, -1f, 1f),
            new Vector3(1f, 0f, 0f), new Vector3(1f, -1f, 1f), new Vector3(1f, -1f, 0f),
            
            //경사면
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0.5f, 1),
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 0.5f, 1)
        };

        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new[]
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
            
            //왼+경사
            30,31,32,
            
            //오른+경사
            33,34,35
        };

        // 각 정점의 노멀 벡터 설정
        mesh.normals = new[]
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

            Vector3.left, Vector3.left, Vector3.left,
            Vector3.forward, Vector3.forward, Vector3.forward,
        };

        // UV 좌표 설정 : unused.
        mesh.uv = new[]
        {
            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,
            
            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,
        };

        SaveMeshAsset("NavMesh_SlopePartial_0", mesh);
    }
    
    [MenuItem("Tools/NavTileMesh/Create/NavMesh_SlopePartial_1")]
    public static void GenerateNavTileMesh_CubeSlope_1()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        /* 시계 방향으로 vertex 찍어야 정방향이다. */
        mesh.vertices = new[]
        {
            // 윗면
            new Vector3(0f, 0.5f, 0f), new Vector3(0f, 1f, 1f), new Vector3(1f, 1f, 1f),
            new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f), new Vector3(1f, 0f, 0f),
            //앞면
            new Vector3(0,0,0), new Vector3(0, 0.5f, 0), new Vector3(1,0,0),
            //뒷면
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1),
            new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
            //왼쪽
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1),
            new Vector3(0, 0, 0), new Vector3(0, 1, 1), new Vector3(0, 0.5f, 0),
            //오른쪽
            new Vector3(1,0,0), new Vector3(1,1,1), new Vector3(1,0,1),
        };

        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new[]
        {
            /*윗면*/
            0, 1, 2,
            3, 4, 5,
            /*앞면*/
            6, 7, 8,
            /*뒷면*/
            9, 10, 11,
            12, 13, 14,
            /*왼쪽면*/
            15, 16, 17,
            18, 19, 20,
            /*오른쪽면*/
            21, 22, 23,
        };

        // 각 정점의 노멀 벡터 설정
        mesh.normals = new[]
        {
            Vector3.up, Vector3.up, Vector3.up,
            Vector3.up, Vector3.up, Vector3.up,
            Vector3.back, Vector3.back, Vector3.back,
            Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.left, Vector3.left, Vector3.left,
            Vector3.left, Vector3.left, Vector3.left,
            Vector3.right, Vector3.right, Vector3.right,
        };

        // UV 좌표 설정 : unused.
        mesh.uv = new[]
        {
            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,
            
            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,
            
            Vector2.zero, Vector2.zero, Vector2.zero,
        };

        SaveMeshAsset("NavMesh_SlopePartial_1", mesh);
    }
    
    [MenuItem("Tools/NavTileMesh/Create/NavMesh_SlopePartial_2")]
    public static void GenerateNavTileMesh_CubeSlope_2()
    {
        Mesh mesh = new Mesh();

        // 정점 좌표 설정 (원점이 윗면의 한 꼭지점이 되도록 설정)
        /* 시계 방향으로 vertex 찍어야 정방향이다. */
        mesh.vertices = new[]
        {
            // 윗면
            new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 1f), new Vector3(1f, 0f, 1f),
            //앞면
            new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(1f, -1f, 0f),
            new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
            //뒷면
            new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 1f), new Vector3(0f, -1f, 1f),
            new Vector3(1f, 0f, 1f), new Vector3(0f, -1f, 1f), new Vector3(1f, -1f, 1f),
            //왼쪽
            new Vector3(0f, 0f, 0f), new Vector3(0f, -1f, 1f), new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 1f),
            //오른쪽
            new Vector3(1f, 0f, 0f), new Vector3(1f, 0, 1f), new Vector3(1f, -1f, 1f),
            new Vector3(1f, 0f, 0f), new Vector3(1f, -1f, 1f), new Vector3(1f, -1f, 0f),
            
            //경사면
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1),
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1)
        };

        // 삼각형을 이루는 인덱스 설정
        mesh.triangles = new[]
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
            
            //왼+경사
            30,31,32,
            
            //오른+경사
            33,34,35
        };

        // 각 정점의 노멀 벡터 설정
        mesh.normals = new[]
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

            Vector3.left, Vector3.left, Vector3.left,
            Vector3.forward, Vector3.forward, Vector3.forward,
        };

        // UV 좌표 설정 : unused.
        mesh.uv = new[]
        {
            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,

            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,
            
            Vector2.zero, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.zero, Vector2.zero,
        };

        SaveMeshAsset("NavMesh_SlopePartial_2", mesh);
    }
}