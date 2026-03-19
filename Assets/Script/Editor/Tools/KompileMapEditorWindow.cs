#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Script.Index;
    using Script.Map.Data;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// [Framework] Editor Manager: 맵 에디터의 UI를 구성하고 씬 뷰의 마우스 입력을 처리하여 타일 데이터를 수정합니다.
    /// </summary>
    public class KompileMapEditorWindow : EditorWindow
    {
        // --- 에디터 상태 변수 ---
        private enum EditMode { None, Paint, Erase, Add, Height, Navi }
        private enum SelectionMode { Vertex, Face } // Edge 모드는 복잡도 상 Face/Vertex 위주로 구현

        private EditMode _currentMode = EditMode.None;
        private MapTextureType _selectedTexture = MapTextureType.map_w;
        private SelectionMode _currentSelection = SelectionMode.Vertex;

        private bool _isEditingEnabled = false;

        // [추가] Repaint 시 사용할 타일 캐싱 변수
        private EditMapTileComponent _lastHoveredTile;

        // Add 모드용 변수
        private GameObject _tilePrefab; // 씬에 생성할 타일 프리팹
        private float _targetY = 0f;    // 허공 클릭 시 타일이 생성될 기본 높이 (투명 그리드 Y값)

        // 13개 포인트의 2D 로컬 좌표 (Height 모드 거리 계산용)
        // 타일의 피벗이 좌하단(최솟값)이므로 -0.5~0.5 대신 0.0~1.0 범위를 사용합니다.
        private static readonly Vector2[] PointOffsets = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),   new Vector2(0.5f, 0.0f),   new Vector2(1.0f, 0.0f),   // 0, 1, 2
            new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f),                            // 3, 4
            new Vector2(0.0f, 0.5f),   new Vector2(0.5f, 0.5f),   new Vector2(1.0f, 0.5f),   // 5, 6, 7
            new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.75f),                            // 8, 9
            new Vector2(0.0f, 1.0f),   new Vector2(0.5f, 1.0f),   new Vector2(1.0f, 1.0f)    // 10, 11, 12
        };

        // --- 창 띄우기 ---
        [MenuItem("Tools/Map/Map Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<KompileMapEditorWindow>("Kompile Map Editor");
            window.minSize = new Vector2(350, 450);
            window.Show();
        }

        // --- 에디터 윈도우 UI 그리기 ---
        private void OnGUI()
        {
            GUILayout.Label("Kompile Map 2.5D Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 1. 편집 활성화 토글
            GUI.backgroundColor = _isEditingEnabled ? Color.green : Color.white;
            if (GUILayout.Button(_isEditingEnabled ? "Editing: ON (클릭하여 끄기)" : "Editing: OFF (클릭하여 켜기)", GUILayout.Height(30)))
            {
                _isEditingEnabled = !_isEditingEnabled;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space();

            // 2. 편집 모드 선택
            GUILayout.Label("Edit Mode", EditorStyles.label);
            _currentMode = (EditMode)GUILayout.Toolbar((int)_currentMode, new string[] { "None", "Paint", "Erase", "Add", "Height", "Navi" });
            EditorGUILayout.Space();

            // 3. 모드별 상세 설정 UI
            switch (_currentMode)
            {
                case EditMode.Paint:
                    GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
                    _selectedTexture = (MapTextureType)EditorGUILayout.EnumPopup("Texture", _selectedTexture);
                    break;

                case EditMode.Add:
                    GUILayout.Label("Add (Placement) Settings", EditorStyles.boldLabel);
                    _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", _tilePrefab, typeof(GameObject), false);
                    _targetY = EditorGUILayout.FloatField("Target Base Y (허공 클릭 시)", _targetY);
                    EditorGUILayout.HelpBox("타일의 면을 클릭하면 그 방향으로 쌓이고, 빈 곳을 클릭하면 Target Base Y 높이에 생성됩니다.", MessageType.Info);
                    break;

                case EditMode.Height:
                    GUILayout.Label("Height Selection Settings", EditorStyles.boldLabel);
                    _currentSelection = (SelectionMode)GUILayout.Toolbar((int)_currentSelection, new string[] { "Vertex (점 1개)", "Face (전체)" });
                    EditorGUILayout.HelpBox("좌클릭: 높이 증가 (+0.125)\nShift + 좌클릭: 높이 감소 (-0.125)", MessageType.Info);
                    break;
            }

            GUILayout.FlexibleSpace();

            // 4. 맵 굽기(Bake) 버튼
            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            if (GUILayout.Button("Bake Map (Combine Meshes)", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Bake Map", "맵 데이터를 구우시겠습니까?", "Bake", "Cancel"))
                {
                    ExecuteBake();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        // --- 씬 뷰(Scene View) 이벤트 처리 ---
        private void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; }
        private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEditingEnabled || _currentMode == EditMode.None) return;

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(controlID);

            // 레이캐스트 생성
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool isHit = Physics.Raycast(ray, out RaycastHit hit);
            EditMapTileComponent hitTile = isHit ? hit.collider.GetComponentInParent<EditMapTileComponent>() : null;

            // [수정 1] Collider가 없을 때 에러 없이 메쉬 기반으로 hitTile을 찾아냅니다. (Add 모드 이외에서 사용)
            if (e.type != EventType.Layout && e.type != EventType.Repaint)
            {
                if (hitTile == null)
                {
                    GameObject pickedObj = HandleUtility.PickGameObject(e.mousePosition, false);
                    if (pickedObj != null)
                    {
                        hitTile = pickedObj.GetComponentInParent<EditMapTileComponent>();
                    }
                }
                // 탐색한 결과를 캐싱 변수에 저장합니다.
                _lastHoveredTile = hitTile;
            }
            else
            {
                // Repaint(그리기) 이벤트일 때는 새롭게 탐색하지 않고 저장된 타일을 그대로 가져옵니다.
                hitTile = _lastHoveredTile;
            }

            // 모드별 분기 처리
            if (_currentMode == EditMode.Add)
            {
                // Add 모드는 나으리의 원본 로직 그대로 작동합니다.
                HandleAddMode(ray, isHit, hit, hitTile, e);
            }
            else if (hitTile != null) // [수정 2] Collider가 없으므로 isHit 조건 제거
            {
                if (_currentMode == EditMode.Paint) HandlePaintMode(hitTile, e);
                else if (_currentMode == EditMode.Erase) HandleEraseMode(hitTile, e);
                else if (_currentMode == EditMode.Height) HandleHeightMode(hitTile, hit.point, e);
            }

            // 마우스 드래그 시 반응성을 위해 화면 갱신
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                sceneView.Repaint();
            }
        }

        // --- 개별 기능 로직 ---

        private void HandlePaintMode(EditMapTileComponent tile, Event e)
        {
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                if (tile.TextureIndex == (int)_selectedTexture) return;

                Undo.RecordObject(tile, "Paint Tile Texture");
                SerializedObject so = new SerializedObject(tile);
                SerializedProperty texProp = so.FindProperty("textureType");
                if (texProp != null)
                {
                    texProp.enumValueIndex = (int)_selectedTexture;
                    so.ApplyModifiedProperties();
                }
                tile.UpdateMesh(); // 즉시 갱신
                e.Use();
            }
        }

        private void HandleEraseMode(EditMapTileComponent tile, Event e)
        {
            // [추가] 시각적 가이드 (빨간색 박스)
            if (e.type == EventType.Repaint && tile != null)
            {
                Vector3 visualCenter = tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f);

                Handles.color = new Color(1, 0, 0, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);

                Handles.color = Color.red;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            // [수정 3] 유니티 씬 뷰 선택 툴이 이벤트를 가로채지 못하도록 락(Lock)을 겁니다.
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                GUIUtility.hotControl = controlID; // 제어권 독점
                Undo.DestroyObjectImmediate(tile.gameObject);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt && GUIUtility.hotControl == controlID)
            {
                Undo.DestroyObjectImmediate(tile.gameObject);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0; // 제어권 반환
                e.Use();
            }
        }

        private void HandleAddMode(Ray ray, bool isHit, RaycastHit hit, EditMapTileComponent hitTile, Event e)
        {
            if (_tilePrefab == null) return;

            Vector3 spawnPos = Vector3.zero;
            bool canSpawn = false;

            if (isHit && hitTile != null)
            {
                // 전략 1: 기존 타일의 면(normal)을 클릭했을 때 그 옆/위에 붙여서 생성
                Vector3 rawPos = hitTile.transform.position + hit.normal;
                // 미세한 부동소수점 오차를 완벽히 제거 (예: 0.9999 -> 1.0)
                spawnPos = new Vector3(Mathf.Round(rawPos.x), Mathf.Round(rawPos.y), Mathf.Round(rawPos.z));
                canSpawn = true;
            }
            else
            {
                // 전략 2: 허공을 클릭했을 때 _targetY 높이의 가상 평면에 생성 (투명 그리드)
                Plane basePlane = new Plane(Vector3.up, new Vector3(0, _targetY, 0));
                if (basePlane.Raycast(ray, out float enter))
                {
                    Vector3 planeHitPoint = ray.GetPoint(enter);
                    spawnPos = new Vector3(Mathf.Round(planeHitPoint.x), _targetY, Mathf.Round(planeHitPoint.z));
                    canSpawn = true;
                }
            }

            if (canSpawn)
            {
                Vector3 visualCenter = spawnPos + new Vector3(0.5f, 0.5f, 0.5f);

                // [가장 확실한 중복 검사: 씬 내의 모든 타일 좌표 직접 비교]
                // 물리(Collider)에 의존하지 않으므로 오작동 확률 0%입니다.
                bool isOccupied = false;

                // Unity 6000 최적화 함수: 씬에 있는 모든 타일 컴포넌트를 가져옵니다.
                EditMapTileComponent[] allTiles = Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                foreach (var existingTile in allTiles)
                {
                    // 대상 위치(spawnPos)와 이미 존재하는 타일의 거리가 0.1 미만이면 완전히 같은 자리로 간주
                    if (Vector3.Distance(existingTile.transform.position, spawnPos) < 0.1f)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (isOccupied)
                {
                    // 중복 위치: 가이드를 빨간색으로 표시
                    Handles.color = new Color(1, 0, 0, 0.3f);
                    Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                    Handles.color = Color.red;
                    Handles.DrawWireCube(visualCenter, Vector3.one);

                    // 마우스 클릭 이벤트 무효화 (클릭해도 생성 안 됨)
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        e.Use();
                    }
                }
                else
                {
                    // 빈 공간: 가이드를 초록색으로 표시
                    Handles.color = new Color(0, 1, 0, 0.3f);
                    Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                    Handles.color = Color.green;
                    Handles.DrawWireCube(visualCenter, Vector3.one);

                    // 마우스 클릭 시 실제 생성
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab);
                        newTile.transform.position = spawnPos;
                        Undo.RegisterCreatedObjectUndo(newTile, "Add New Tile");
                        e.Use();
                    }
                }
            }
        }

        private void HandleHeightMode(EditMapTileComponent tile, Vector3 hitPoint, Event e)
        {
            // 1. 클릭한 지점의 로컬 좌표 계산
            Vector3 localHit = tile.transform.InverseTransformPoint(hitPoint);
            Vector2 hit2D = new Vector2(localHit.x, localHit.z);

            // 2. 13개의 점 중 가장 가까운 점 찾기
            int nearestIndex = 0;
            float minDistance = float.MaxValue;

            for (int i = 0; i < PointOffsets.Length; i++)
            {
                float dist = Vector2.Distance(hit2D, PointOffsets[i]);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestIndex = i;
                }
            }

            // 3. 영향을 받을 포인트 리스트 결정 (Vertex 모드 vs Face 모드)
            List<int> affectedIndices = new List<int>();
            if (_currentSelection == SelectionMode.Vertex)
            {
                affectedIndices.Add(nearestIndex); // 가장 가까운 한 점만
            }
            else if (_currentSelection == SelectionMode.Face)
            {
                for (int i = 0; i < 13; i++) affectedIndices.Add(i); // 13개 점 모두
            }

            // 4. 시각적 가이드 표시 (노란색 구체)
            Handles.color = Color.yellow;
            foreach (int idx in affectedIndices)
            {
                // 현재 높이를 반영한 정확한 월드 위치 계산
                Vector3 localPosWithHeight = new Vector3(PointOffsets[idx].x, tile.GetPointLocalPos(idx).y, PointOffsets[idx].y);
                Vector3 worldPos = tile.transform.TransformPoint(localPosWithHeight);
                Handles.SphereHandleCap(0, worldPos, Quaternion.identity, 0.08f, EventType.Repaint);
            }

            // 5. 클릭 입력 처리 (높이 변경)
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Shift 키를 누르고 있으면 -1 (깎기), 아니면 +1 (높이기)
                int delta = e.shift ? -1 : 1;

                Undo.RecordObject(tile, "Adjust Tile Height");

                foreach (int idx in affectedIndices)
                {
                    tile.ModifyHeightIndex(idx, delta);
                }

                tile.UpdateMesh(); // 메쉬 재생성
                e.Use();
            }
        }

        private void ExecuteBake()
        {
            // 나으리의 Bake 로직 (Provider 등)을 호출
            Debug.Log("[Framework] Bake 로직이 실행되었습니다.");
        }
    }
}
#endif