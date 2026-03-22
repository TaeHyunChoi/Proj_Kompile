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
    /// [Framework] Editor Manager: 타일셋 팔레트, 스포이드, Focus Mode(타겟 외 타일 딤 처리)를 지원하는 통합 맵 에디터입니다.
    /// </summary>
    public class KompileMapEditorWindow : EditorWindow
    {
        private enum EditMode { None, Paint, Erase, Add, Height, Navi }
        private enum SelectionMode { Vertex, Face }

        private EditMode _currentMode = EditMode.None;
        private SelectionMode _currentSelection = SelectionMode.Vertex;

        private bool _isEditingEnabled = false;
        private bool _isAltPressed = false;

        // 13개 포인트 정의
        private static readonly Vector2[] PointOffsets = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),   new Vector2(0.5f, 0.0f),   new Vector2(1.0f, 0.0f),
            new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f),
            new Vector2(0.0f, 0.5f),   new Vector2(0.5f, 0.5f),   new Vector2(1.0f, 0.5f),
            new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.75f),
            new Vector2(0.0f, 1.0f),   new Vector2(0.5f, 1.0f),   new Vector2(1.0f, 1.0f)
        };

        // --- 배치 및 레이어 설정 ---
        private GameObject _tilePrefab;
        private float _targetY = 0f;
        private ushort _targetRenderLayer = 0;

        // --- 시각화 설정 ---
        private bool _focusSelectedLayer = false;

        // --- 팔레트 데이터 ---
        private List<TileSetDefinition> _allTileSets = new List<TileSetDefinition>();
        private List<TileSetDefinition> _filteredTileSets = new List<TileSetDefinition>();
        private TileSetDefinition _selectedTileSet;
        private string _searchString = "";
        private Vector2 _paletteScrollPos;
        private EditMapTileComponent _lastHoveredTile;

        [MenuItem("Tools/Map/Map Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<KompileMapEditorWindow>("Kompile Map Editor");
            window.minSize = new Vector2(350, 550);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshLibraryData();
            UpdateTilesFocusState(); // 윈도우 열릴 때 포커스 상태 동기화
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            ClearAllTilesFocusState(); // 윈도우 닫힐 때 모든 딤(Dim) 처리 해제
        }

        // [신규] 씬 내의 타일들의 어둡기(Dim) 상태를 갱신합니다.
        private void UpdateTilesFocusState()
        {
            var allTiles = Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var tile in allTiles)
            {
                bool shouldDim = _focusSelectedLayer && (tile.RenderLayer != _targetRenderLayer);
                tile.SetVisualDimmed(shouldDim);
            }
            SceneView.RepaintAll();
        }

        // [신규] 모든 타일의 어둡기 상태를 원상 복구합니다.
        private void ClearAllTilesFocusState()
        {
            var allTiles = Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var tile in allTiles)
            {
                tile.SetVisualDimmed(false);
            }
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
            _filteredTileSets = string.IsNullOrEmpty(_searchString)
                ? new List<TileSetDefinition>(_allTileSets)
                : _allTileSets.Where(x => x.name.ToLower().Contains(_searchString.ToLower())).ToList();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout) UpdateFilteredList();

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

            // --- View Options ---
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("View Options", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _focusSelectedLayer = EditorGUILayout.ToggleLeft("🔍 Focus Target Layer (해당 레이어 외 어둡게 표시)", _focusSelectedLayer);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateTilesFocusState(); // 토글 변경 시 즉시 타일 색상 갱신
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            _currentMode = (EditMode)GUILayout.Toolbar((int)_currentMode, new string[] { "None", "Paint", "Erase", "Add", "Height", "Navi" });
            EditorGUILayout.Space();

            if (_currentMode != EditMode.None && _currentMode != EditMode.Navi)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Layer Settings", EditorStyles.boldLabel);
                _targetY = EditorGUILayout.FloatField("Target Base Y (제한 층)", _targetY);

                EditorGUI.BeginChangeCheck();
                int tempLayer = EditorGUILayout.IntField("Target Render Layer", _targetRenderLayer);
                _targetRenderLayer = (ushort)Mathf.Clamp(tempLayer, 0, ushort.MaxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    // 타겟 레이어가 변경되면 어두워질 대상도 바뀌므로 갱신
                    if (_focusSelectedLayer) UpdateTilesFocusState();
                }

                EditorGUILayout.HelpBox($"현재 {_targetY}층 / 타겟 렌더 레이어 {_targetRenderLayer} 에서 작업 중입니다.", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            switch (_currentMode)
            {
                case EditMode.Paint: DrawPaletteUI(); break;
                case EditMode.Add: _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", _tilePrefab, typeof(GameObject), false); break;
                case EditMode.Height: _currentSelection = (SelectionMode)GUILayout.Toolbar((int)_currentSelection, new string[] { "Vertex", "Face" }); break;
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
            if (GUILayout.Button("🔄 리로드", GUILayout.Width(80))) RefreshLibraryData();
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _searchString = EditorGUILayout.TextField("Search", _searchString);
            if (EditorGUI.EndChangeCheck()) UpdateFilteredList();

            EditorGUILayout.Space();

            _paletteScrollPos = EditorGUILayout.BeginScrollView(_paletteScrollPos, GUILayout.Height(180));
            int columnCount = Mathf.Max(1, (int)(position.width / 110f));
            if (columnCount < 1) columnCount = 1;

            for (int i = 0; i < _filteredTileSets.Count; i += columnCount)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < columnCount; j++)
                {
                    int index = i + j; if (index >= _filteredTileSets.Count) break;
                    var ts = _filteredTileSets[index];
                    Color oldColor = GUI.backgroundColor;
                    if (_selectedTileSet == ts) GUI.backgroundColor = Color.cyan;
                    if (GUILayout.Button(ts.name, GUILayout.Width(100), GUILayout.Height(35))) { _selectedTileSet = ts; EditorGUIUtility.PingObject(ts); }
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
            if (e.alt != _isAltPressed) { _isAltPressed = e.alt; sceneView.Repaint(); Repaint(); }

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
                GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
                if (picked != null)
                {
                    var found = picked.GetComponentInParent<EditMapTileComponent>();
                    if (found != null && Mathf.Abs(found.transform.position.y - _targetY) < 0.1f) hitTile = found;
                }
                _lastHoveredTile = hitTile;
            }
            else hitTile = _lastHoveredTile;

            if (_currentMode == EditMode.Add) HandleAddMode(ray, e);
            else if (hitTile != null)
            {
                if (_currentMode == EditMode.Paint) HandlePaintMode(hitTile, e, controlID);
                else if (_currentMode == EditMode.Erase) HandleEraseMode(hitTile, e, controlID);
                else if (_currentMode == EditMode.Height) HandleHeightMode(hitTile, ray, e, controlID);
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) sceneView.Repaint();
        }

        private void HandleAddMode(Ray ray, Event e)
        {
            if (_tilePrefab == null) return;
            Plane p = new Plane(Vector3.up, new Vector3(0, _targetY, 0));
            if (!p.Raycast(ray, out float enter)) return;
            Vector3 spawnPos = new Vector3(Mathf.Round(ray.GetPoint(enter).x), _targetY, Mathf.Round(ray.GetPoint(enter).z));

            if (e.type == EventType.Repaint)
            {
                DrawCustomGrid(spawnPos);
                Vector3 visualCenter = spawnPos + new Vector3(0.5f, 0.5f, 0.5f);

                bool isOccupied = Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Any(t => Vector3.Distance(t.transform.position, spawnPos) < 0.1f);

                Handles.color = isOccupied ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = isOccupied ? Color.red : Color.green;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (!Physics.OverlapBox(spawnPos + Vector3.one * 0.5f, Vector3.one * 0.4f).Any())
                {
                    GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab);
                    newTile.transform.position = spawnPos;

                    var comp = newTile.GetComponent<EditMapTileComponent>();
                    if (comp != null)
                    {
                        comp.SetRenderLayer(_targetRenderLayer);
                        // 생성될 때 포커스 모드면 타겟 레이어이므로 어두워지지 않아야 함
                        comp.SetVisualDimmed(false);
                    }

                    Undo.RegisterCreatedObjectUndo(newTile, "Add Tile");
                    e.Use();
                }
            }
        }

        private void HandlePaintMode(EditMapTileComponent tile, Event input, int controlID)
        {
            bool isEyedropper = input.alt;
            Vector3 visualCenter = tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f);

            if (input.type == EventType.Repaint)
            {
                if (isEyedropper)
                {
                    Handles.color = new Color(1f, 0f, 1f, 0.3f);
                    Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                    Handles.color = Color.magenta;
                    Handles.DrawWireCube(visualCenter, Vector3.one);

                    GUIStyle labelStyle = new GUIStyle(EditorStyles.helpBox);
                    labelStyle.fontStyle = FontStyle.Bold; labelStyle.normal.textColor = Color.white; labelStyle.alignment = TextAnchor.MiddleCenter;
                    Handles.Label(tile.transform.position + new Vector3(0.5f, 1.2f, 0.5f), "🎨 스포이드", labelStyle);
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
                if (input.type == EventType.MouseDown && input.button == 0 && tile.TileSet != null)
                {
                    _selectedTileSet = tile.TileSet;
                    EditorGUIUtility.PingObject(_selectedTileSet);
                    Repaint();
                    input.Use();
                }
                return;
            }

            if (input.button != 0 || _selectedTileSet == null) return;

            if (input.type == EventType.MouseDown || (input.type == EventType.MouseDrag && GUIUtility.hotControl == controlID))
            {
                GUIUtility.hotControl = controlID;
                tile.ApplyTileSet(_selectedTileSet);
                input.Use();
            }
            else if (input.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                input.Use();
            }
        }

        private void HandleEraseMode(EditMapTileComponent tile, Event e, int controlID)
        {
            if (e.type == EventType.Repaint)
            {
                Handles.color = new Color(1, 0, 0, 0.3f);
                Handles.CubeHandleCap(0, tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, 1.05f, EventType.Repaint);
                Handles.color = Color.red;
                Handles.DrawWireCube(tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one);
            }

            if (e.button != 0 || e.alt) return;

            if (e.type == EventType.MouseDown) { GUIUtility.hotControl = controlID; Undo.DestroyObjectImmediate(tile.gameObject); e.Use(); }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == controlID) { Undo.DestroyObjectImmediate(tile.gameObject); e.Use(); }
            else if (e.type == EventType.MouseUp) { GUIUtility.hotControl = 0; e.Use(); }
        }

        private void HandleHeightMode(EditMapTileComponent tile, Ray ray, Event e, int controlID)
        {
            Plane p = new Plane(tile.transform.up, tile.transform.position);
            if (!p.Raycast(ray, out float enter)) return;

            Vector3 localHit = tile.transform.InverseTransformPoint(ray.GetPoint(enter));
            Vector2 hit2D = new Vector2(localHit.x, localHit.z);
            int nearIdx = 0; float floatMinDist = float.MaxValue;

            for (int i = 0; i < PointOffsets.Length; i++)
            {
                float d = Vector2.Distance(hit2D, PointOffsets[i]);
                if (d < floatMinDist) { floatMinDist = d; nearIdx = i; }
            }

            if (e.type == EventType.Repaint)
            {
                DrawCustomGrid(tile.transform.position);
                Handles.color = Color.yellow;
                foreach (int idx in (_currentSelection == SelectionMode.Vertex ? new int[] { nearIdx } : Enumerable.Range(0, 13)))
                    Handles.SphereHandleCap(0, tile.transform.TransformPoint(new Vector3(PointOffsets[idx].x, tile.GetPointLocalPos(idx).y, PointOffsets[idx].y)), Quaternion.identity, 0.08f, EventType.Repaint);
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                GUIUtility.hotControl = controlID;
                int delta = e.shift ? -1 : 1;
                Undo.RecordObject(tile, "Adjust Height");
                foreach (int idx in (_currentSelection == SelectionMode.Vertex ? new int[] { nearIdx } : Enumerable.Range(0, 13)))
                    tile.ModifyHeightIndex(idx, delta);
                tile.UpdateMesh();
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        private void DrawCustomGrid(Vector3 pos)
        {
            float y = _targetY; float sX = Mathf.Floor(pos.x) - 1f; float sZ = Mathf.Floor(pos.z) - 1f;
            Handles.color = new Color(1, 1, 1, 0.2f);
            for (float i = -1; i <= 2; i++)
            {
                Handles.DrawLine(new Vector3(sX, y, sZ + i), new Vector3(sX + 3f, y, sZ + i));
                Handles.DrawLine(new Vector3(sX + i, y, sZ), new Vector3(sX + i, y, sZ + 3f));
            }
        }

        private void ExecuteBake() { Debug.Log("Bake 실행"); }
    }
}
#endif