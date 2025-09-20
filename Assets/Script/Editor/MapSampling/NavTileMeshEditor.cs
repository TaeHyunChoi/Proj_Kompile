#if UNITY_EDITOR
using Script.Data;
using static Script.Index.MapTileIndex;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

public static class NavTileMeshEditor
{
    private static readonly float3[] VerticeVerticePoint = new float3[]
{
        new float3(0f,    0f,    0f   ),   new float3(0.5f,  0f,    0f   ),   new float3(1f,    0f,    0f),
        new float3(0.25f, 0f,    0.25f),   new float3(0.75f, 0f,    0.25f),   new float3(0f,    0f,    0.5f),
        new float3(0.5f,  0f,    0.5f ),   new float3(1f,    0f,    0.5f ),   new float3(0.25f, 0f,    0.75f),
        new float3(0.75f, 0f,    0.75f),   new float3(0f,    0f,    1f   ),   new float3(0.5f,  0f,    1f),
        new float3(1f,    0f,    1f   )
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
            heightFlag = (-1 == height) ? HEIGHT_MASK : (ulong)height;
            heightMask |= heightFlag << i * HEIGHT_BITS;
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


        // reset(clear) vertice, uv
        Vector3[] vertices = new Vector3[length];
        Vector2[] uv = new Vector2[length];

        // set: vertice, uv
        // virtualIndex: NavTileMesh를 만들기 위해 개념적으로 사용하는 가상 순서 (입력 순서와 동일)

        Vector3 vertice;
        int height;

        int triangleMask = TRIANGLE_FULL_MASK;
        int verticeIndex = 0;


        for (int virtualIndex = 0; virtualIndex < length; ++virtualIndex)
        {
            height = inputHeights[virtualIndex];

            vertice = VerticeVerticePoint[virtualIndex];

            if (height >= 0)
            {
                vertice += HEIGHT_UNIT_VALUE * height * Vector3.up;
            }
            else
            {
                // 삼각형 대상에서 제외
                triangleMask &= ExceptTriangleMask[virtualIndex];
            }

            vertices[verticeIndex] = vertice;
            uv[verticeIndex] = new Vector2(vertice.x, vertice.z);
            ++verticeIndex;
        }

        // set: triangle
        int flag = 1;
        length = GetTriangleCount(triangleMask) * 3;
        int[] triangle = new int[length];
        int triangleIndex = 0;
        int t_index = 0;
        while (flag <= triangleMask)
        {
            if (0 != (flag & triangleMask))
            {
                int index = triangleIndex * 3;
                triangle[t_index    ] = TriangleVertex[index + 0];
                triangle[t_index + 1] = TriangleVertex[index + 1];
                triangle[t_index + 2] = TriangleVertex[index + 2];
                t_index += 3;
            }

            flag <<= 1;
            ++triangleIndex;
        }

        // instantiate: mesh
        Mesh mesh = new Mesh()
        {
            vertices  = vertices,
            triangles = triangle,
            uv = uv
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
    private static int GetTriangleCount(int mask)
    {
        int count = 0;
        int flag  = 1;

        while (flag < mask)
        {
            if (0 != (flag & mask))
            {
                count += 1;
            }

            flag <<= 1;
        }

        return count;
    }
}
#endif // UNITY_EDITOR
