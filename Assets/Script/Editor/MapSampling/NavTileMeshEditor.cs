#if UNITY_EDITOR
using Script.Data;
using Script.Util;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class NavTileMeshEditor
{
    private static readonly Vector3[] VerticeVerticePoint = new Vector3[]
{
        new Vector3(0f,    0f,    0f   ),   new Vector3(0.5f,  0f,    0f   ),   new Vector3(1f,    0f,    0f),
        new Vector3(0.25f, 0f,    0.25f),   new Vector3(0.75f, 0f,    0.25f),   new Vector3(0f,    0f,    0.5f),
        new Vector3(0.5f,  0f,    0.5f ),   new Vector3(1f,    0f,    0.5f ),   new Vector3(0.25f, 0f,    0.75f),
        new Vector3(0.75f, 0f,    0.75f),   new Vector3(0f,    0f,    1f   ),   new Vector3(0.5f,  0f,    1f),
        new Vector3(1f,    0f,    1f   )
};
    private static readonly (int, int, int)[] VirtualTriangleVertex = new (int, int, int)[]
    {
        (  0,  3,  1), (  1,  3,  6), (  3,  5,  6),
        (  0,  5,  3), (  1,  4,  2), (  2,  4,  7),
        (  4,  6,  7), (  1,  6,  4), (  5,  8,  6),
        (  6,  8, 11), (  8, 10, 11), (  5, 10,  8),
        (  6,  9,  7), (  7,  9, 12), (  9, 11, 12),
        (  6, 11,  9)
    };
    private static readonly int[] ExceptTriangleMask = new int[]
{
        TRIANGLE_FULL_MASK & ~(1 <<  0 | 1 <<  3),
        TRIANGLE_FULL_MASK & ~(1 <<  0 | 1 <<  1 | 1 <<  4 | 1 <<  7),
        TRIANGLE_FULL_MASK & ~(1 <<  4 | 1 <<  5),                 
        TRIANGLE_FULL_MASK & ~(1 <<  0 | 1 <<  1 | 1 <<  2 | 1 <<  3),
        TRIANGLE_FULL_MASK & ~(1 <<  4 | 1 <<  5 | 1 <<  6 | 1 <<  7),
        TRIANGLE_FULL_MASK & ~(1 <<  2 | 1 <<  3 | 1 <<  8 | 1 << 11),
        TRIANGLE_FULL_MASK & ~(1 <<  1 | 1 <<  2 | 1 <<  6 | 1 <<  7 | 1 <<  8 | 1 <<  9 | 1 << 12 | 1 << 15),
        TRIANGLE_FULL_MASK & ~(1 <<  5 | 1 <<  6 | 1 << 12 | 1 << 13),
        TRIANGLE_FULL_MASK & ~(1 <<  8 | 1 <<  9 | 1 << 10 | 1 << 11),
        TRIANGLE_FULL_MASK & ~(1 << 12 | 1 << 13 | 1 << 14 | 1 << 15),
        TRIANGLE_FULL_MASK & ~(1 << 10 | 1 << 11),
        TRIANGLE_FULL_MASK & ~(1 <<  9 | 1 << 10 | 1 << 14 | 1 << 15),
        TRIANGLE_FULL_MASK & ~(1 << 13 | 1 << 14)
};

    private static Vector3[] _vertice;
    private static Vector2[] _uv;
    private static int[]     _triangle;

    private const float HEIGHT_UNIT_VALUE  = 0.125f;    // 높이값의 단위. (height * 0.125f). 0 ~ 1의 값을 가진다.
    private const int   TRIANGLE_FULL_MASK = 0x_FFFF;   // 하나의 mesh 안에 4*4, 16개의 triangle로 이뤄졌다.

    private static StringBuilder stringBuilder = new StringBuilder();

    public static void SaveData(string fileName, bool isSmall, int[] heights)
    {
        int height;
        ulong heightFlag;
        ulong heightMask = 0;
        for (int i = 0; i < heights.Length; ++i)
        {
            height = heights[i];
            heightFlag = (-1 == height) ? MapUtil.HEIGHT_MASK : (ulong)height;
            heightMask |= heightFlag << i * MapUtil.HEIGHT_BITS;
        }

        stringBuilder.Append(fileName).Append("_").Append(heightMask);
        fileName = stringBuilder.ToString();
        stringBuilder.Clear();

        // set file name
        if (true == isSmall)
        {
            fileName += "_s";
        }

        Mesh mesh = InstantiateMesh(heights);

        // create | save mesh
        var path = "Assets/Rcs/NavTile/Mesh/NavTileMesh_" + fileName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) is not null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Mesh asset created: " + fileName);

        // create|save prefab for test
        stringBuilder.Append("Assets/Editor/Prefab/");
        stringBuilder.Append($"{fileName}");
        stringBuilder.Append(".prefab");

        path = stringBuilder.ToString();
        stringBuilder.Clear();

        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) is not null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        bool isSuccess;
        GameObject prefabObject = new GameObject(fileName);
        {
            var filter   = prefabObject.AddComponent<MeshFilter>();
            filter.mesh  = mesh;
            var renderer = prefabObject.AddComponent<MeshRenderer>();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Editor/Texture/mat_NavTile_00.mat"); //임의로 생성한 매터리얼
            renderer.sharedMaterial = material;

            // 타일 정보를 유니티의 NavMesh를 Bake하는 것처럼 데이터 저장할 때 호출하는 함수
            var maptilePrefab = prefabObject.AddComponent<EditMapData>();
            maptilePrefab.InitializePrefab(heights, isSmall);

            PrefabUtility.SaveAsPrefabAsset(prefabObject, path, out isSuccess);
        }

        if (true == isSuccess)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Success to Create NavTile Prefab: " + fileName);
        }
        else
        {
            Debug.LogError("Fail to Create NavTile Prefab: " + fileName);
        }

        //testPrefab을 Scene에 생성했으나 필요 없으니 없앤다.
        Object.DestroyImmediate(prefabObject);
    }

    /// <summary> 입력받은 높이값[13]을 Mesh 정보로 변환하여 저장 </summary>
    public static Mesh InstantiateMesh(int[] inputHeights)
    {
        int length        = inputHeights.Length;
        int triangleMask  = TRIANGLE_FULL_MASK;
        int verticeIndex  = 0;
        int triangleIndex = 0;

        // reset(clear) vertice, uv
        _vertice   = _vertice.Reset(length); 
        _uv        = _uv.Reset(length);

        // set: vertice, uv
        // virtualIndex: NavTileMesh를 만들기 위해 개념적으로 사용하는 가상 순서 (입력 순서와 동일)
        for (int virtualIndex = 0; virtualIndex < length; ++virtualIndex)
        {
            int h = inputHeights[virtualIndex];

            if (h >= 0)
            {
                Vector3 vertex = VerticeVerticePoint[virtualIndex] + HEIGHT_UNIT_VALUE * h * Vector3.up;
                _vertice[verticeIndex] = vertex;
                _uv[verticeIndex]      = new Vector2(vertex.x, vertex.z);

                ++verticeIndex;
            }
            else
            {
                // 삼각형 대상에서 제외
                triangleMask &= ExceptTriangleMask[virtualIndex];
            }
        }

        // reset(clear) triangle
        length = GetTriangleCount(triangleMask) * 3;
        _triangle = _triangle.Reset(length);

        // set: triangle
        verticeIndex = 0;
        int flag = 1;

        while (flag < triangleMask)
        {
            if (0 != (flag & triangleMask))
            {
                _triangle[verticeIndex]     = VirtualTriangleVertex[triangleIndex].Item1;
                _triangle[verticeIndex + 1] = VirtualTriangleVertex[triangleIndex].Item2;
                _triangle[verticeIndex + 2] = VirtualTriangleVertex[triangleIndex].Item3;

                verticeIndex += 3;
            }

            flag <<= 1;
            ++triangleIndex;
        }

        // instantiate: mesh
        Mesh mesh = new Mesh()
        {
            vertices  = _vertice,
            triangles = _triangle,
            uv        = _uv
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
    private static int GetTriangleCount(int mask)
    {
        int count = 0;
        int shift = 0;

        int flag  = 1;
        while (flag < mask)
        {
            if (0 != (flag & mask))
            {
                count += 1;
            }

            ++shift;
            flag <<= 1;
        }

        return count;
    }
}
#endif // UNITY_EDITOR
