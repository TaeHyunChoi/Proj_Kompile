#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Script.Index;
    using Script.Map.Data;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// [Framework] Editor Manager: 팔레트 기반 타일셋 선택, 직관적인 스포이드(Alt) UX, Y축 층 제한 편집을 지원하는 통합 맵 에디터입니다.
    /// </summary>
    public class KompileMapEditorWindow : EditorWindow
    {
        private enum EditMode { None, Paint, Erase, Add, Height, Navi }
        private enum SelectionMode { Vertex, Face }

        private EditMode _currentMode = EditMode.None;
        private SelectionMode _currentSelection = SelectionMode.Vertex;

        private bool _isEditingEnabled = false;
        private bool _isAltPressed = false;
        private EditMapTileComponent _lastHoveredTile;

        private GameObject _tilePrefab;
        private float _targetY = 0f;

        private List<TileSetDefinition> _allTileSets = new List<TileSetDefinition>();
        private List<TileSetDefinition> _filteredTileSets = new List<TileSetDefinition>();
        private TileSetDefinition _selectedTileSet;
        private string _searchString = "";
        private Vector2 _paletteScrollPos;

        private static readonly Vector2[] PointOffsets = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),   new Vector2(0.5f, 0.0f),   new Vector2(1.0f, 0.0f),
            new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f),
            new Vector2(0.0f, 0.5f),   new Vector2(0.5f, 0.5f),   new Vector2(1.0f, 0.5f),
            new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.75f),
            new Vector2(0.0f, 1.0f),   new Vector2(0.5f, 1.0f),   new Vector2(1.0f, 1.0f)
        };

        [MenuItem("Tools/Map/Map Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<KompileMapEditorWindow>("Kompile Map Editor");
            window.minSize = new Vector2(350, 500);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshLibraryData();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void RefreshLibraryData()
        {
            _allTileSets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:TileSetDefinition");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TileSetDefinition>(path);
                if (asset != null) _allTileSets.Add(asset);
            }
            UpdateFilteredList();
        }

        private void UpdateFilteredList()
        {
            if (string.IsNullOrEmpty(_searchString))
            {
                _filteredTileSets = new List<TileSetDefinition>(_allTileSets);
            }
            else
            {
                _filteredTileSets = _allTileSets
                    .Where(x => x.name.ToLower().Contains(_searchString.ToLower()))
                    .ToList();
            }
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                UpdateFilteredList();
            }

            GUILayout.Label("Kompile Map 2.5D Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUI.backgroundColor = _isEditingEnabled ? Color.green : Color.white;
            if (GUILayout.Button(_isEditingEnabled ? "Editing: ON (클릭하여 끄기)" : "Editing: OFF (클릭하여 켜기)", GUILayout.Height(30)))
            {
                _isEditingEnabled = !_isEditingEnabled;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space();

            _currentMode = (EditMode)GUILayout.Toolbar((int)_currentMode, new string[] { "None", "Paint", "Erase", "Add", "Height", "Navi" });
            EditorGUILayout.Space();

            if (_currentMode != EditMode.None && _currentMode != EditMode.Navi)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Layer Settings", EditorStyles.boldLabel);
                _targetY = EditorGUILayout.FloatField("Target Base Y (제한 층)", _targetY);
                EditorGUILayout.HelpBox($"현재 {_targetY}층에서만 작업이 가능합니다.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            switch (_currentMode)
            {
                case EditMode.Paint:
                    DrawPaletteUI();
                    break;

                case EditMode.Add:
                    _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", _tilePrefab, typeof(GameObject), false);
                    break;

                case EditMode.Height:
                    _currentSelection = (SelectionMode)GUILayout.Toolbar((int)_currentSelection, new string[] { "Vertex", "Face" });
                    break;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Bake Map (Combine Meshes)", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Bake Map", "맵 데이터를 구우시겠습니까?", "Bake", "Cancel")) ExecuteBake();
            }
        }

        private void DrawPaletteUI()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Brush Palette", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 리로드", GUILayout.Width(80)))
            {
                RefreshLibraryData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _searchString = EditorGUILayout.TextField("Search", _searchString);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredList();
            }

            EditorGUILayout.Space();

            _paletteScrollPos = EditorGUILayout.BeginScrollView(_paletteScrollPos, GUILayout.Height(180));
            int columnCount = Mathf.Max(1, (int)(position.width / 110f));

            for (int i = 0; i < _filteredTileSets.Count; i += columnCount)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < columnCount; j++)
                {
                    int index = i + j;
                    if (index >= _filteredTileSets.Count) break;

                    var ts = _filteredTileSets[index];

                    Color oldColor = GUI.backgroundColor;
                    if (_selectedTileSet == ts) GUI.backgroundColor = Color.cyan;

                    if (GUILayout.Button(ts.name, GUILayout.Width(100), GUILayout.Height(35)))
                    {
                        _selectedTileSet = ts;
                        EditorGUIUtility.PingObject(ts);
                    }
                    GUI.backgroundColor = oldColor;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            _selectedTileSet = (TileSetDefinition)EditorGUILayout.ObjectField("Selected Brush", _selectedTileSet, typeof(TileSetDefinition), false);

            if (_isAltPressed)
            {
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.magenta;
                EditorGUILayout.HelpBox("스포이드 모드 활성화됨! 씬에서 타일을 클릭하여 추출하세요.", MessageType.Info);
                GUI.backgroundColor = oldColor;
            }
            else
            {
                EditorGUILayout.HelpBox("단축키: 씬에서 Alt + 클릭으로 타일셋을 추출(스포이드)할 수 있습니다.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEditingEnabled || _currentMode == EditMode.None) return;

            Event e = Event.current;

            // Alt 키 상태 감지 및 즉각 갱신
            if (e.alt != _isAltPressed)
            {
                _isAltPressed = e.alt;
                sceneView.Repaint();
                Repaint();
            }

            // [오류 수정] 존재하지 않는 EyeDropper 대신 ArrowPlus 커서 사용
            if (_currentMode == EditMode.Paint && _isAltPressed)
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, sceneView.position.width, sceneView.position.height), MouseCursor.ArrowPlus);
            }

            int controlID = GUIUtility.GetControlID("KompileMapEditor".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(controlID);

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            EditMapTileComponent hitTile = null;

            if (e.type != EventType.Layout && e.type != EventType.Repaint)
            {
                GameObject pickedObj = HandleUtility.PickGameObject(e.mousePosition, false);
                if (pickedObj != null)
                {
                    var foundTile = pickedObj.GetComponentInParent<EditMapTileComponent>();
                    if (foundTile != null && Mathf.Abs(foundTile.transform.position.y - _targetY) < 0.01f)
                    {
                        hitTile = foundTile;
                    }
                }
                _lastHoveredTile = hitTile;
            }
            else
            {
                hitTile = _lastHoveredTile;
            }

            if (_currentMode == EditMode.Add)
            {
                HandleAddMode(ray, e);
            }
            else if (hitTile != null)
            {
                if (_currentMode == EditMode.Paint) HandlePaintMode(hitTile, e, controlID);
                else if (_currentMode == EditMode.Erase) HandleEraseMode(hitTile, e, controlID);
                else if (_currentMode == EditMode.Height) HandleHeightMode(hitTile, ray, e, controlID);
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) sceneView.Repaint();
        }

        private void DrawCustomGrid(Vector3 pivotPos)
        {
            float y = _targetY;
            float startX = Mathf.Floor(pivotPos.x) - 1f;
            float startZ = Mathf.Floor(pivotPos.z) - 1f;

            Handles.color = new Color(1f, 1f, 1f, 0.4f);
            for (int i = 0; i <= 3; i++)
            {
                Handles.DrawLine(new Vector3(startX, y, startZ + i), new Vector3(startX + 3f, y, startZ + i));
                Handles.DrawLine(new Vector3(startX + i, y, startZ), new Vector3(startX + i, y, startZ + 3f));
            }
        }

        private void HandlePaintMode(EditMapTileComponent tile, Event input, int controlID)
        {
            bool isEyedropper = input.alt;

            if (input.type == EventType.Repaint)
            {
                Vector3 visualCenter = tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f);

                if (isEyedropper)
                {
                    Handles.color = new Color(1f, 0f, 1f, 0.3f);
                    Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                    Handles.color = Color.magenta;
                    Handles.DrawWireCube(visualCenter, Vector3.one);

                    GUIStyle labelStyle = new GUIStyle(EditorStyles.helpBox);
                    labelStyle.fontStyle = FontStyle.Bold;
                    labelStyle.normal.textColor = Color.white;
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    Handles.Label(tile.transform.position + new Vector3(0.5f, 1.2f, 0.5f), "🎨 스포이드 추출", labelStyle);
                }
                else
                {
                    Handles.color = new Color(0, 1, 1, 0.3f);
                    Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                    Handles.color = Color.cyan;
                    Handles.DrawWireCube(visualCenter, Vector3.one);
                }
            }

            if (isEyedropper)
            {
                if (input.type == EventType.MouseDown && input.button == 0)
                {
                    if (tile.TileSet != null)
                    {
                        _selectedTileSet = tile.TileSet;
                        EditorGUIUtility.PingObject(_selectedTileSet);
                        Repaint();
                        input.Use();
                    }
                }
                return;
            }

            if (input.button != 0) return;
            if (_selectedTileSet == null) return;

            if (input.type == EventType.MouseDown)
            {
                GUIUtility.hotControl = controlID;
                tile.ApplyTileSet(_selectedTileSet);
                input.Use();
            }
            else if (input.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
            {
                tile.ApplyTileSet(_selectedTileSet);
                input.Use();
            }
            else if (input.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                input.Use();
            }
        }

        private void HandleEraseMode(EditMapTileComponent tile, Event e, int controlID)
        {
            if (e.type == EventType.Repaint && tile != null)
            {
                Vector3 visualCenter = tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f);
                Handles.color = new Color(1, 0, 0, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = Color.red;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            if (e.button != 0 || e.alt) return;

            if (e.type == EventType.MouseDown)
            {
                GUIUtility.hotControl = controlID;
                Undo.DestroyObjectImmediate(tile.gameObject);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
            {
                Undo.DestroyObjectImmediate(tile.gameObject);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        private void HandleAddMode(Ray ray, Event e)
        {
            if (_tilePrefab == null) return;

            Plane targetPlane = new Plane(Vector3.up, new Vector3(0, _targetY, 0));
            if (!targetPlane.Raycast(ray, out float enter)) return;

            Vector3 planeHitPoint = ray.GetPoint(enter);
            Vector3 spawnPos = new Vector3(Mathf.Round(planeHitPoint.x), _targetY, Mathf.Round(planeHitPoint.z));

            if (e.type == EventType.Repaint) DrawCustomGrid(spawnPos);

            Vector3 visualCenter = spawnPos + new Vector3(0.5f, 0.5f, 0.5f);

            if (e.type == EventType.Repaint)
            {
                bool isOccupied = false;
                EditMapTileComponent[] allTiles = Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var existingTile in allTiles)
                {
                    if (Vector3.Distance(existingTile.transform.position, spawnPos) < 0.1f) { isOccupied = true; break; }
                }

                if (isOccupied)
                {
                    Handles.color = new Color(1, 0, 0, 0.3f);
                    Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                    Handles.color = Color.red;
                    Handles.DrawWireCube(visualCenter, Vector3.one);
                }
                else
                {
                    Handles.color = new Color(0, 1, 0, 0.3f);
                    Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                    Handles.color = Color.green;
                    Handles.DrawWireCube(visualCenter, Vector3.one);
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                var existing = Physics.OverlapBox(spawnPos + Vector3.one * 0.5f, Vector3.one * 0.4f);
                if (existing.Length == 0)
                {
                    GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab);
                    newTile.transform.position = spawnPos;
                    Undo.RegisterCreatedObjectUndo(newTile, "Add New Tile");
                    e.Use();
                }
            }
        }

        private void HandleHeightMode(EditMapTileComponent tile, Ray ray, Event e, int controlID)
        {
            Plane tileBasePlane = new Plane(tile.transform.up, tile.transform.position);
            if (!tileBasePlane.Raycast(ray, out float enter)) return;

            Vector3 worldHitPoint = ray.GetPoint(enter);
            Vector3 localHitPoint = tile.transform.InverseTransformPoint(worldHitPoint);
            Vector2 hit2D = new Vector2(localHitPoint.x, localHitPoint.z);

            int nearestIndex = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < PointOffsets.Length; i++)
            {
                float dist = Vector2.Distance(hit2D, PointOffsets[i]);
                if (dist < minDistance) { minDistance = dist; nearestIndex = i; }
            }

            List<int> affectedIndices = new List<int>();
            if (_currentSelection == SelectionMode.Vertex) affectedIndices.Add(nearestIndex);
            else for (int i = 0; i < 13; i++) affectedIndices.Add(i);

            if (e.type == EventType.Repaint)
            {
                DrawCustomGrid(tile.transform.position);
                Handles.color = Color.yellow;
                foreach (int idx in affectedIndices)
                {
                    Vector3 pPos = new Vector3(PointOffsets[idx].x, tile.GetPointLocalPos(idx).y, PointOffsets[idx].y);
                    Handles.SphereHandleCap(0, tile.transform.TransformPoint(pPos), Quaternion.identity, 0.08f, EventType.Repaint);
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                GUIUtility.hotControl = controlID;
                int delta = e.shift ? -1 : 1;
                bool isAnyChanged = false;
                Undo.RecordObject(tile, "Adjust Tile Height");
                foreach (int idx in affectedIndices)
                {
                    float currentH = tile.GetPointLocalPos(idx).y;
                    float nextH = currentH + (delta * 0.125f);
                    if (nextH >= -0.01f && nextH <= 1.01f) { tile.ModifyHeightIndex(idx, delta); isAnyChanged = true; }
                }

                if (isAnyChanged) tile.UpdateMesh();
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        private void ExecuteBake() { Debug.Log("[Framework] Bake 로직이 실행되었습니다."); }
    }
}
#endif