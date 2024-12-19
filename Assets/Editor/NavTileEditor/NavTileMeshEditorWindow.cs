using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NavTileMeshEditorWindow : EditorWindow
{
    private const float heightPerUnit = 0.125f;
    
    private int[] inputHeights = new int[13]; // 12개의 int 값
    private string inputFileName = "default_name"; // 파일 이름
    private bool isSmall = false;

    [MenuItem("Tools/Custom Editor Window")]
    public static void ShowWindow()
    {
        GetWindow<NavTileMeshEditorWindow>("Nav Tile Mesh Editor");
    }

    private void OnGUI()
    {
        // File name input
        GUILayout.Space(5);
        var boldLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold // 글씨를 굵게 설정
        };
        EditorGUILayout.LabelField("File Name", boldLabelStyle); // 레이블을 굵게 설정
        inputFileName = EditorGUILayout.TextField(inputFileName);
        isSmall = GUILayout.Toggle(isSmall, "Is Small");

        GUILayout.Space(5);
        GUILayout.Label("Input Values", EditorStyles.boldLabel);
        
        float fieldWidth = 50; // IntField의 너비
        float spacing    = 10;   // 필드 간의 간격
        float startX     = 20;    // 시작 X 위치
        float startY     = 85;    // 시작 Y 위치

        DrawInputRow(new[] { "h10", "h11", "h12" }, new[] { 10, 11, 12 }, startX, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h8", "h9" }, new[] { 8, 9 }, startX + fieldWidth + spacing, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h5", "h6", "h7" }, new[] { 5, 6, 7 }, startX, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h3", "h4" }, new[] { 3, 4 }, startX + fieldWidth + spacing, startY, fieldWidth, spacing);
        startY += 30;

        DrawInputRow(new[] { "h0", "h1", "h2" }, new[] { 0, 1, 2 }, startX, startY, fieldWidth, spacing);
        GUILayout.Space(startY - 30);
        
        if (GUILayout.Button("Save Mesh"))
        {
            SaveData();
        }
        GUILayout.Space(1);
        if (GUILayout.Button("Clear Height"))
        {
            GUI.FocusControl(null);
            
            for (var i = 0; i < inputHeights.Length; i++)
            {
                inputHeights[i] = 0;
            }
        }
        
    }

    private void DrawInputRow(string[] labels, int[] indices, float startX, float startY, float fieldWidth, float spacing)
    {
        float currentX = startX;

        for (int i = 0; i < labels.Length; i++)
        {
            // Label
            EditorGUI.LabelField(new Rect(currentX, startY, 30, 20), labels[i]);
            currentX += 30;

            // IntField
            inputHeights[indices[i]] = EditorGUI.IntField(new Rect(currentX, startY, fieldWidth, 20), inputHeights[indices[i]]);
            currentX += fieldWidth + spacing;
        }
    }

    private void SaveData()
    {
        string fName = inputFileName;
        if (true == isSmall)
        {
            fName += "_s";
        }

        if (false == TrySetMeshFields(out Vector3[] points, out int triangleFlag))
        {
            Debug.LogError("Fail to Mesh asset created[TrySetMeshFields]: " + fName);
            return;
        }

        if (false == TrySetMesh(points, triangleFlag, out Mesh mesh))
        {
            Debug.LogError("Fail to Mesh asset created[TrySetMesh]: " + fName);
            return;
        }

        // create|save mesh
        var path = "Assets/Rcs/NavTile/Mesh/NavTileMesh_" + fName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) is not null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Mesh asset created: " + fName);
        
        // create|save prefab for test
        path = "Assets/Editor/Prefab/nav_" + fName + "_test.prefab";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) is not null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        bool isSuccess;
        var testPrefab = new GameObject(fName);
        {
            var filter = testPrefab.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            var renderer = testPrefab.AddComponent<MeshRenderer>();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Rcs/Material/Mat_Inner.mat");
            renderer.sharedMaterial = material;

            var navTileMesh = testPrefab.AddComponent<NavTileMesh>();
            navTileMesh.InitNaviMask(inputHeights, isSmall);
            
            PrefabUtility.SaveAsPrefabAsset(testPrefab, path, out isSuccess);
        }


        if (true == isSuccess)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Success to Create NavTile Prefab: " + fName);
        }
        else
        {
            Debug.LogError("Fail to Create NavTile Prefab: " + fName);
        }
        
        DestroyImmediate(testPrefab);
    }

    private bool TrySetMeshFields(out Vector3[] points, out int triangleMask)
    {
        points = new Vector3[13];
        triangleMask = 0xFFFF;
        
        for (var i = 0; i < inputHeights.Length; i++)
        {
            Vector3 vertex;
            switch (i)
            {
                case  0: vertex = new Vector3(0f,    0f, 0f   ); break;
                case  1: vertex = new Vector3(0.5f,  0f, 0f   ); break;
                case  2: vertex = new Vector3(1f,    0f, 0f   ); break;
                case  3: vertex = new Vector3(0.25f, 0f, 0.25f); break;
                case  4: vertex = new Vector3(0.75f, 0f, 0.25f); break;
                case  5: vertex = new Vector3(0f,    0f, 0.5f ); break;
                case  6: vertex = new Vector3(0.5f,  0f, 0.5f ); break;
                case  7: vertex = new Vector3(1f,    0f, 0.5f ); break;
                case  8: vertex = new Vector3(0.25f, 0f, 0.75f); break;
                case  9: vertex = new Vector3(0.75f, 0f, 0.75f); break;
                case 10: vertex = new Vector3(0f,    0f, 1f   ); break;
                case 11: vertex = new Vector3(0.5f,  0f, 1f   ); break;
                case 12: vertex = new Vector3(1f,    0f, 1f   ); break;
                default:
                    Debug.LogError("Invalid index[TrySetVertices]: " + i);
                    return false;
            }

            var h = inputHeights[i];
            if (h >= 0)
            {
                var scale = isSmall ? 0.5f : 1f;
                var height = h * heightPerUnit;
                points[i] = scale * (vertex + new Vector3(0, height, 0));
            }
            else
            {
                // h = -1;
                triangleMask = this[i, triangleMask]; // indexer
                points[i] = new Vector3(-1f, -1f, -1f);
            }
        }
        return true;
    }
    private int this[int except, int triangleFlag]
    {
        get
        {
            var full = 0xFFFFF;
            switch (except)
            {
                case 0:
                    triangleFlag &= full & ~(1 << 0);
                    triangleFlag &= full & ~(1 << 3);
                    break;
                case 1:
                    triangleFlag &= full & ~(1 << 0);
                    triangleFlag &= full & ~(1 << 1);
                    triangleFlag &= full & ~(1 << 4);
                    triangleFlag &= full & ~(1 << 7);
                    break;
                case 2:
                    triangleFlag &= full & ~(1 << 4);
                    triangleFlag &= full & ~(1 << 5);
                    break;
                case 3:
                    triangleFlag &= full & ~(1 << 0);
                    triangleFlag &= full & ~(1 << 1);
                    triangleFlag &= full & ~(1 << 2);
                    triangleFlag &= full & ~(1 << 3);
                    break;
                case 4:
                    triangleFlag &= full & ~(1 << 4);
                    triangleFlag &= full & ~(1 << 5);
                    triangleFlag &= full & ~(1 << 6);
                    triangleFlag &= full & ~(1 << 7);
                    break;
                case 5:
                    triangleFlag &= full & ~(1 << 2);
                    triangleFlag &= full & ~(1 << 3);
                    triangleFlag &= full & ~(1 << 8);
                    triangleFlag &= full & ~(1 << 11);
                    break;
                case 6:
                    triangleFlag &= full & ~(1 << 1);
                    triangleFlag &= full & ~(1 << 2);
                    triangleFlag &= full & ~(1 << 6);
                    triangleFlag &= full & ~(1 << 7);
                    triangleFlag &= full & ~(1 << 8);
                    triangleFlag &= full & ~(1 << 9);
                    triangleFlag &= full & ~(1 << 12);
                    triangleFlag &= full & ~(1 << 15);
                    break;
                case 7:
                    triangleFlag &= full & ~(1 << 5);
                    triangleFlag &= full & ~(1 << 6);
                    triangleFlag &= full & ~(1 << 12);
                    triangleFlag &= full & ~(1 << 13);
                    break;
                case 8:
                    triangleFlag &= full & ~(1 << 8);
                    triangleFlag &= full & ~(1 << 9);
                    triangleFlag &= full & ~(1 << 10);
                    triangleFlag &= full & ~(1 << 11);
                    break;
                case 9:
                    triangleFlag &= full & ~(1 << 12);
                    triangleFlag &= full & ~(1 << 13);
                    triangleFlag &= full & ~(1 << 14);
                    triangleFlag &= full & ~(1 << 15);
                    break;
                case 10:
                    triangleFlag &= full & ~(1 << 10);
                    triangleFlag &= full & ~(1 << 11);
                    break;
                case 11:
                    triangleFlag &= full & ~(1 << 9);
                    triangleFlag &= full & ~(1 << 10);
                    triangleFlag &= full & ~(1 << 14);
                    triangleFlag &= full & ~(1 << 15);
                    break;
                case 12:
                    triangleFlag &= full & ~(1 << 13);
                    triangleFlag &= full & ~(1 << 14);
                    break;
                default:
                    Debug.LogError("Invalid index[ExceptTriangle]: " + except);
                    return -1;
            }

            return triangleFlag;
        }
    }

    
    private bool TrySetMesh(Vector3[] points, int triangleFlag, out Mesh mesh)
    {
        mesh = null;
        
        var flag = triangleFlag;
        var triangles = new List<int>();
        int triangleIndex = 0;
        
        while (flag != 0)
        {
            if (0 != (flag & 1))
            {
                switch (triangleIndex)
                {
                    case 0: triangles.Add(0); triangles.Add(3); triangles.Add(1); break;
                    case 1: triangles.Add(1); triangles.Add(3); triangles.Add(6); break;
                    case 2: triangles.Add(3); triangles.Add(5); triangles.Add(6); break;
                    case 3: triangles.Add(0); triangles.Add(5); triangles.Add(3); break;
                    case 4: triangles.Add(1); triangles.Add(4); triangles.Add(2); break;
                    case 5: triangles.Add(2); triangles.Add(4); triangles.Add(7); break;
                    case 6: triangles.Add(4); triangles.Add(6); triangles.Add(7); break;
                    case 7: triangles.Add(1); triangles.Add(6); triangles.Add(4); break;
                    case 8: triangles.Add(5); triangles.Add(8); triangles.Add(6); break;
                    case 9: triangles.Add(6); triangles.Add(8); triangles.Add(11); break;
                    case 10: triangles.Add(8); triangles.Add(10); triangles.Add(11); break;
                    case 11: triangles.Add(5); triangles.Add(10); triangles.Add(8); break;
                    case 12: triangles.Add(6); triangles.Add(9); triangles.Add(7); break;
                    case 13: triangles.Add(7); triangles.Add(9); triangles.Add(12); break;
                    case 14: triangles.Add(9); triangles.Add(11); triangles.Add(12); break;
                    case 15: triangles.Add(6); triangles.Add(11); triangles.Add(9); break;
                    default:
                        Debug.LogError("Invalid triangle index[" + triangleIndex + "]: " );
                        return false;
                }
            }

            ++triangleIndex;
            flag >>= 1;
        }

        var uvs = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            uvs[i] = new Vector2(points[i].x, points[i].z);
        }

        mesh = new Mesh()
        {
            vertices = points,
            triangles = triangles.ToArray(),
            uv = uvs
        };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return true;
    }
}
