using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Script.Data;

// 자료구조: VirtualVertex
public partial class NavTileMeshEditorWindow
{
    private struct VirtualVertex
    {
        private const float HEIGHT_UNIT_VALUE = 0.125f;    //높이값의 단위. (height * 0.125f). 0 ~ 1의 값을 가진다.

        public readonly int     Index;
        public readonly Vector3 Vertex;

        /// <param name="index">실제로 Mesh.triangles에 저장된 인덱스(순서)</param>
        /// <param name="vIndex">입력값(inputHeights)의 인덱스(순서)</param>
        /// <param name="height">입력한 높이값</param>
        public VirtualVertex(int index, int vIndex, float height)
        {
            Index   = index;

            Vector3 vertex = new Vector3(0f, height * HEIGHT_UNIT_VALUE, 0f);
            switch (vIndex)
            {
                case  0: vertex += new Vector3(0f,    0f, 0f   ); break;
                case  1: vertex += new Vector3(0.5f,  0f, 0f   ); break;
                case  2: vertex += new Vector3(1f,    0f, 0f   ); break;
                case  3: vertex += new Vector3(0.25f, 0f, 0.25f); break;
                case  4: vertex += new Vector3(0.75f, 0f, 0.25f); break;
                case  5: vertex += new Vector3(0f,    0f, 0.5f ); break;
                case  6: vertex += new Vector3(0.5f,  0f, 0.5f ); break;
                case  7: vertex += new Vector3(1f,    0f, 0.5f ); break;
                case  8: vertex += new Vector3(0.25f, 0f, 0.75f); break;
                case  9: vertex += new Vector3(0.75f, 0f, 0.75f); break;
                case 10: vertex += new Vector3(0f,    0f, 1f   ); break;
                case 11: vertex += new Vector3(0.5f,  0f, 1f   ); break;
                case 12: vertex += new Vector3(1f,    0f, 1f   ); break;
                default: break;
            }

            Vertex = vertex;
        }
    }
}

// Custom Editor + Create Asset
public partial class NavTileMeshEditorWindow : EditorWindow
{
    private const int   TRIANGLE_FULL_MASK = 0b_1111_1111_1111_1111; //0xFFFF
    
    private int[]  inputHeights  = new int[13];    // 각 vertex의 height 입력값
    private string inputFileName = "default_name"; // 에셋 파일 이름
    private bool   isSmall       = false;          // (실내 등) 타일맵을 작게 만들 때 체크

    // 커스텀 에디터 표기
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
        GUILayout.Label("Input Vertex Height", EditorStyles.boldLabel);
        
        float fieldWidth = 50; // IntField의 너비
        float spacing    = 10; // 필드 간의 간격
        float startX     = 20; // 시작 X 위치
        float startY     = 85; // 시작 Y 위치

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
        // set file name
        string fName = inputFileName;
        if (true == isSmall)
        {
            fName += "_s";
        }

        // parse (input -> virtual -> mesh)
        if (false == TryParse(out List<Vector3> vertice, out List<int> triangle, out List<Vector2> uv))
        {
            Debug.LogError("Fail to Mesh asset created[TrySetMeshFields]: " + fName);
            return;
        }

        // set mesh data
        // [comment] 나에게 필요한 값은 vertices, triangles 2개이지만
        // 에디터에서 보기 좋게 확인하려고 uv, bounds, normals 데이터도 추가 (오직 에디터에서만 사용하는 데이터)
        Mesh mesh = new Mesh()
        {
            vertices = vertice.ToArray(),
            triangles = triangle.ToArray(),
            uv = uv.ToArray()
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // create | save mesh
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
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Editor/Texture/mat_NavTile_00.mat"); //임의로 생성한 매터리얼
            renderer.sharedMaterial = material;

            // 타일 정보를 유니티의 NavMesh를 Bake하는 것처럼 데이터 저장할 때 호출하는 함수
            var navTileMesh = testPrefab.AddComponent<EditMapNavData>();
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
        
        //testPrefab을 Scene에 생성했으나 필요 없으니 없앤다.
        DestroyImmediate(testPrefab);
    }

    /// <summary> 입력받은 높이값[13]을 Mesh 정보로 변환하여 저장 </summary>
    private bool TryParse(out List<Vector3> vertice, out List<int> triangle, out List<Vector2> uv)
    {
        int triangleMask = TRIANGLE_FULL_MASK;

        // virtual vertice + vertice, uv
        int length = inputHeights.Length;
        VirtualVertex[] virtualVertices = new VirtualVertex[length];
        VirtualVertex   virtualVertex;
        vertice = new List<Vector3>();
        uv      = new List<Vector2>();

        // 가상 정보 생성 (Mesh.vertices 입력 순서와 입력 입력 순서의 싱크를 맞춤)
        {
            int mi = 0; // (mesh index) Mesh에 저장되는 실제 버텍스 순서 
            int vi;     // (virtual index) NavTileMesh를 만들기 위해 개념적으로 사용하는 가상 순서 (입력 순서와 동일)

            for (vi = 0; vi < length; ++vi)
            {
                int h = inputHeights[vi];

                if (h >= 0)
                {
                    virtualVertex = new VirtualVertex(mi, vi, h);
                    Vector3 vertex = virtualVertex.Vertex;
                    vertice.Add(vertex);
                    uv.Add(new Vector2(vertex.x, vertex.z));
                    ++mi;
                }
                else
                {
                    virtualVertex = new VirtualVertex(-1, -1, 0);
                    triangleMask = GetTriangleMaskExcept(triangleMask, except: vi);
                }

                virtualVertices[vi] = virtualVertex;
            }
        }

        // triangle
        triangle = new List<int>();
        {
            int index = 0;
            while (triangleMask > 0)
            {
                if (0 == (triangleMask & 1))
                {
                    goto NEXT_LOOP;
                }

                int vt1, vt2, vt3;
                switch (index)
                {
                    case  0: vt1 =  0; vt2 =  3; vt3 =  1; break;
                    case  1: vt1 =  1; vt2 =  3; vt3 =  6; break;
                    case  2: vt1 =  3; vt2 =  5; vt3 =  6; break;
                    case  3: vt1 =  0; vt2 =  5; vt3 =  3; break;
                    case  4: vt1 =  1; vt2 =  4; vt3 =  2; break;
                    case  5: vt1 =  2; vt2 =  4; vt3 =  7; break;
                    case  6: vt1 =  4; vt2 =  6; vt3 =  7; break;
                    case  7: vt1 =  1; vt2 =  6; vt3 =  4; break;
                    case  8: vt1 =  5; vt2 =  8; vt3 =  6; break;
                    case  9: vt1 =  6; vt2 =  8; vt3 = 11; break;
                    case 10: vt1 =  8; vt2 = 10; vt3 = 11; break;
                    case 11: vt1 =  5; vt2 = 10; vt3 =  8; break;
                    case 12: vt1 =  6; vt2 =  9; vt3 =  7; break;
                    case 13: vt1 =  7; vt2 =  9; vt3 = 12; break;
                    case 14: vt1 =  9; vt2 = 11; vt3 = 12; break;
                    case 15: vt1 =  6; vt2 = 11; vt3 =  9; break;
                    default: return false;
                }

                triangle.Add(virtualVertices[vt1].Index);
                triangle.Add(virtualVertices[vt2].Index);
                triangle.Add(virtualVertices[vt3].Index);

            NEXT_LOOP:
                triangleMask >>= 1;
                ++index;
            }
        }

        return true;
    }

    /// <summary> 높이값에 -1을 입력하면 꼭지점에서 제외. 
    /// 꼭지점 1개라도 없으면 삼각형을 만들 수 없으므로 삼각형 대상에서 제외 </summary>
    private int GetTriangleMaskExcept(int mask, int except)
    {
        switch (except)
        {
            case 0:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 0);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 3);
                break;
            case 1:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 0);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 1);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 4);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 7);
                break;
            case 2:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 4);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 5);
                break;
            case 3:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 0);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 1);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 2);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 3);
                break;
            case 4:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 4);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 5);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 6);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 7);
                break;
            case 5:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 2);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 3);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 8);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 11);
                break;
            case 6:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 1);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 2);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 6);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 7);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 8);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 9);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 12);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 15);
                break;
            case 7:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 5);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 6);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 12);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 13);
                break;
            case 8:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 8);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 9);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 10);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 11);
                break;
            case 9:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 12);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 13);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 14);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 15);
                break;
            case 10:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 10);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 11);
                break;
            case 11:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 9);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 10);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 14);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 15);
                break;
            case 12:
                mask &= TRIANGLE_FULL_MASK & ~(1 << 13);
                mask &= TRIANGLE_FULL_MASK & ~(1 << 14);
                break;
            default:
                Debug.LogError("Invalid index[ExceptTriangle]: " + except);
                return -1;
        }

        return mask;
    }
}
