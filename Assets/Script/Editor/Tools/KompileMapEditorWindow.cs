#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Script.Map.Data;
    using Script.Map.Entity;
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Editor Manager: 멀티 아틀라스 팔레트, 스포이드, Focus Mode를 지원하는 통합 맵 에디터.
    /// [고도화] 각 텍스처 폴더별 독립적인 MapTextureTable을 읽어와 아틀라스 인덱스를 낭비 없이 관리합니다.
    /// </summary>
    public class KompileMapEditorWindow : EditorWindow
    {
        private enum EditMode { None, Paint, Erase, Add, Height, Navi }
        private enum SelectionMode { Vertex, Face }

        private EditMode _currentMode = EditMode.None;
        private SelectionMode _currentSelection = SelectionMode.Vertex;

        private bool _isEditingEnabled = false;
        private bool _isAltPressed = false;

        private static readonly Vector2[] PointOffsets = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),   new Vector2(0.5f, 0.0f),   new Vector2(1.0f, 0.0f),
            new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f),
            new Vector2(0.0f, 0.5f),   new Vector2(0.5f, 0.5f),   new Vector2(1.0f, 0.5f),
            new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.75f),
            new Vector2(0.0f, 1.0f),   new Vector2(0.5f, 1.0f),   new Vector2(1.0f, 1.0f)
        };

        private EditMapSamplingComponent _samplingRoot;
        private GameObject _tilePrefab;
        
        private float _targetY = 0f;
        private ushort _targetRenderLayer = 0;
        private bool _focusSelectedLayer = false;

        private EditMapTileComponent _lastHoveredTile;
        private Vector2 _mainScrollPos = Vector2.zero;

        private class AtlasPage
        {
            public string PageName;
            public Texture2D Texture;
            public int[] GlobalIndices = new int[64];
        }

        private List<AtlasPage> _atlasPages = new List<AtlasPage>();
        private int _selectedAtlasPageIndex = 0;

        private const string ROOT_INPUT_PATH = "Assets/Rcs/Map";

        private int _brushTopIndex = 0;
        private Texture2D _brushTopAtlas = null;
        private int _brushSideIndex = 0;
        private Texture2D _brushSideAtlas = null;

        private HashSet<EditMapTileComponent> _cachedTiles = new HashSet<EditMapTileComponent>();

        [MenuItem("Tools/Map/Map Editor")]
        public static void ShowWindow() => GetWindow<KompileMapEditorWindow>("Kompile Map Editor").Show();

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            LoadAllAtlases();
            RefreshTileCache();
            UpdateTilesFocusState();

            if (_samplingRoot == null)
            {
                _samplingRoot = UnityEngine.Object.FindFirstObjectByType<EditMapSamplingComponent>();
            }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            ClearAllTilesFocusState();
        }

        private void LoadAllAtlases()
        {
            _atlasPages.Clear();
            _selectedAtlasPageIndex = 0;

            if (false == Directory.Exists(ROOT_INPUT_PATH))
            {
                return;
            }
            
            string[] directories = Directory.GetDirectories(ROOT_INPUT_PATH);
            foreach (string dir in directories)
            {
                string folderName = new DirectoryInfo(dir).Name;
                string tablePath = $"{dir}/MapTextureTable.asset";
                MapTextureTable textureTable = AssetDatabase.LoadAssetAtPath<MapTextureTable>(tablePath);

                if (false == textureTable)
                {
                    Debug.LogWarning($"[Framework] {folderName} 폴더에 MapTextureTable 에셋이 없습니다. 병합을 먼저 실행해주세요.");
                    continue;
                }

                string[] allFiles = Directory.GetFiles(dir, "*.png");
                List<string> filteredFiles = new List<string>();

                foreach (string f in allFiles)
                {
                    string fileName = Path.GetFileName(f);
    
                    // "merged-"로 시작하지 않는 파일만 추가
                    if (!fileName.StartsWith("merged-"))
                    {
                        filteredFiles.Add(f);
                    }
                }

                Dictionary<int, string> validFiles = new Dictionary<int, string>();
                foreach (string file in allFiles)
                {
                    string fileNameNoExt = Path.GetFileNameWithoutExtension(file);
                    var existingData = textureTable.TextureList.Find(x => x.TextureName.Equals(fileNameNoExt, System.StringComparison.OrdinalIgnoreCase));
                    if (existingData != null)
                    {
                        validFiles[existingData.GlobalIndex] = file;                        
                    }
                }

                var groupedFiles = validFiles.GroupBy(kvp => kvp.Key >> 6)
                                                                .OrderBy(g => g.Key)
                                                                .ToList();
                foreach (var group in groupedFiles)
                {
                    int atlasPageNum = group.Key;
                    string suffix = (atlasPageNum == 0) ? "" : $"-{atlasPageNum}";
                    string atlasPath = $"{dir}/merged-{folderName}{suffix}.png";

                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                    if (true == tex)
                    {
                        AtlasPage page = new AtlasPage
                        {
                            PageName = $"{folderName}{suffix}", 
                            Texture = tex
                        };
                        for (int j = 0; j < 64; j++)
                        {
                            page.GlobalIndices[j] = -1;
                        }
                        
                        foreach (var kvp in group)
                        {
                            int localIndex = kvp.Key % 64;
                            page.GlobalIndices[localIndex] = kvp.Key;
                        }
                        _atlasPages.Add(page);

                        if (false == _brushTopAtlas)
                        {
                            _brushTopAtlas = tex;                            
                        }

                        if (false == _brushSideAtlas)
                        {
                            _brushSideAtlas = tex;
                        }
                    }
                }
            }
        }

        private void RefreshTileCache()
        {
            _cachedTiles.Clear();
            var allTiles = UnityEngine.Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var t in allTiles)
            {
                _cachedTiles.Add(t);                
            }
        }

        private void UpdateTilesFocusState()
        {
            foreach (EditMapTileComponent tile in _cachedTiles)
            {
                if (true == tile)
                {
                    bool dim = (true == _focusSelectedLayer) && (tile.RenderLayer != _targetRenderLayer);
                    tile.SetVisualDimmed(dim);                    
                }
            }
            SceneView.RepaintAll();
        }

        private void ClearAllTilesFocusState()
        {
            foreach (var tile in _cachedTiles)
            {
                if (true == tile)
                {
                    tile.SetVisualDimmed(false);                    
                }
            }
        }

        private void OnGUI()
        {
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

            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("View Options", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _focusSelectedLayer = EditorGUILayout.ToggleLeft("🔍 Focus Target Layer (해당 레이어 외 어둡게 표시)", _focusSelectedLayer);
            if (EditorGUI.EndChangeCheck()) UpdateTilesFocusState();

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
                if (EditorGUI.EndChangeCheck() && _focusSelectedLayer) UpdateTilesFocusState();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            switch (_currentMode)
            {
                case EditMode.Paint:
                    DrawAtlasPaletteUI();
                    break;
                case EditMode.Add:
                    _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", _tilePrefab, typeof(GameObject), false);
                    _samplingRoot = (EditMapSamplingComponent)EditorGUILayout.ObjectField("Sampling Root", _samplingRoot, typeof(EditMapSamplingComponent), true);
                    break;
                case EditMode.Height: _currentSelection = (SelectionMode)GUILayout.Toolbar((int)_currentSelection, new string[] { "Vertex", "Face" }); break;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Optimize Visible Sides (옆면 최적화)", GUILayout.Height(30))) ExecuteOptimizeMesh();

            if (GUILayout.Button("Bake Map (Combine Meshes)", GUILayout.Height(40)))
            {
                ExecuteOptimizeMesh();
                if (EditorUtility.DisplayDialog("Bake Map", "맵 데이터를 구우시겠습니까?", "Bake", "Cancel"))
                {
                    ExecuteBake();
                }
            }
        }

        private void DrawAtlasPaletteUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Multi Atlas Palette", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 리로드", GUILayout.Width(80)))
            {
                LoadAllAtlases();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_atlasPages.Count == 0)
            {
                EditorGUILayout.HelpBox("아틀라스 텍스처를 찾을 수 없습니다.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            string[] pageNames = _atlasPages.Select(p => $"[{p.PageName}] Atlas").ToArray();
            _selectedAtlasPageIndex = EditorGUILayout.Popup("Select Theme", _selectedAtlasPageIndex, pageNames);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("💡 [아틀라스 픽업 조작법]\n• 좌클릭 : 윗면(Top) 지정\n• 우클릭 : 옆면(Side) 지정", MessageType.Info);
            EditorGUILayout.Space();

            AtlasPage currentPage = _atlasPages[_selectedAtlasPageIndex];
            float atlasSize = Mathf.Min(position.width - 30f, 300f);
            Rect atlasRect = GUILayoutUtility.GetRect(atlasSize, atlasSize, GUILayout.ExpandWidth(false));
            atlasRect.x += (position.width - atlasSize - 20f) * 0.5f;

            GUI.DrawTexture(atlasRect, currentPage.Texture, ScaleMode.ScaleToFit);

            Event e = Event.current;
            float cellSize = atlasSize / 8f;
            int hoveredIndex = -1;
            int hoveredGlobalIndex = -1;

            if (atlasRect.Contains(e.mousePosition))
            {
                float cellSize_recip = 1 / cellSize;
                int col = Mathf.FloorToInt((e.mousePosition.x - atlasRect.x) * cellSize_recip);
                int row = Mathf.FloorToInt((e.mousePosition.y - atlasRect.y) *  cellSize_recip);
                hoveredIndex = row * 8 + col;
                hoveredGlobalIndex = currentPage.GlobalIndices[hoveredIndex];

                if (e.type == EventType.MouseDown && hoveredGlobalIndex != -1)
                {
                    if (e.button == 0)
                    {
                        _brushTopIndex = hoveredGlobalIndex;
                        _brushTopAtlas = currentPage.Texture;
                        e.Use();
                    }
                    else if (e.button == 1)
                    {
                        _brushSideIndex = hoveredGlobalIndex;
                        _brushSideAtlas = currentPage.Texture;
                        e.Use();
                    }
                }
            }

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Rect cellRect = new Rect(atlasRect.x + c * cellSize, atlasRect.y + r * cellSize, cellSize, cellSize);
                    int localIndex = r * 8 + c;
                    int globalIndex = currentPage.GlobalIndices[localIndex];

                    if (globalIndex == -1)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0, 0, 0, 0.5f));
                        continue;
                    }

                    if (globalIndex == _brushTopIndex && _brushTopAtlas == currentPage.Texture)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0, 1, 0, 0.4f));
                        GUI.Label(new Rect(cellRect.x, cellRect.y, cellSize, 15), " T", EditorStyles.whiteMiniLabel);
                    }
                    if (globalIndex == _brushSideIndex && _brushSideAtlas == currentPage.Texture)
                    {
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellSize / 2, cellSize, cellSize / 2), new Color(0, 0.5f, 1, 0.4f));
                        GUI.Label(new Rect(cellRect.x, cellRect.y + cellSize - 15, cellSize, 15), " S", EditorStyles.whiteMiniLabel);
                    }

                    if (localIndex == hoveredIndex && globalIndex != -1)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(1, 1, 1, 0.2f));                        
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"🖌️ Current Brush Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawBrushPreview("Top (윗면)", _brushTopIndex, _brushTopAtlas);
            GUILayout.Space(20);
            DrawBrushPreview("Side (옆면)", _brushSideIndex, _brushSideAtlas);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();

            if (_isAltPressed)
            {
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.magenta;
                EditorGUILayout.HelpBox("스포이드 모드: 씬에서 타일을 클릭하여 텍스처를 추출하세요.", MessageType.Info);
                GUI.backgroundColor = oldColor;
            }
            else
            {
                EditorGUILayout.HelpBox("단축키: 씬에서 Alt + 클릭으로 맵에 깔린 타일의 텍스처를 스포이드 할 수 있습니다.", MessageType.None);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBrushPreview(string label, int index, Texture2D atlas)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label($"Idx: {index}", EditorStyles.centeredGreyMiniLabel);

            Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
            rect.x += (80 - 64) / 2f;
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

            if (true == atlas)
            {
                int localIndex = index % 64;
                int col = localIndex % 8;
                int row = localIndex / 8;
                Rect uvRect = new Rect(col / 8f, 1f - ((row + 1) / 8f), 1f / 8f, 1f / 8f);
                GUI.DrawTextureWithTexCoords(rect, atlas, uvRect);
            }
            else
            {
                GUI.Label(rect, "Empty", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEditingEnabled || _currentMode == EditMode.None)
            {
                return;
            }

            Event e = Event.current;
            if (e.alt != _isAltPressed)
            {
                _isAltPressed = e.alt; 
                sceneView.Repaint(); Repaint();
            }

            if (_currentMode == EditMode.Paint && _isAltPressed)
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, sceneView.position.width, sceneView.position.height), MouseCursor.ArrowPlus);
            }

            int controlID = GUIUtility.GetControlID("KompileMapEditor".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }
            
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            EditMapTileComponent hitTile = null;

            if (e.type != EventType.Layout && e.type != EventType.Repaint)
            {
                float minDistanceToVertex = 40f;
                EditMapTileComponent bestTile = null;

                foreach (var tile in _cachedTiles)
                {
                    if (false == tile)
                    {
                        continue;                        
                    }
                    if (_focusSelectedLayer && tile.RenderLayer != _targetRenderLayer)
                    {
                        continue;
                    }

                    if (false == _isAltPressed && Mathf.Abs(tile.transform.position.y - _targetY) > 0.1f)
                    {
                        continue;
                    }

                    for (int i = 0; i < 13; i++)
                    {
                        Vector3 pointPos = tile.transform.TransformPoint(new Vector3(PointOffsets[i].x, tile.GetPointLocalPos(i).y, PointOffsets[i].y));
                        Vector2 guiPoint = HandleUtility.WorldToGUIPoint(pointPos);
                        float dist = Vector2.Distance(guiPoint, e.mousePosition);

                        if (dist < minDistanceToVertex)
                        {
                            minDistanceToVertex = dist;
                            bestTile = tile;
                        }
                    }
                }
                hitTile = bestTile;

                if (_lastHoveredTile != hitTile)
                {
                    sceneView.Repaint();
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
            else if (true == hitTile)
            {
                if (_currentMode == EditMode.Paint)
                {
                    HandlePaintMode(hitTile, e, controlID);
                }
                else if (_currentMode == EditMode.Erase)
                {
                    HandleEraseMode(hitTile, e, controlID);
                }
                else if (_currentMode == EditMode.Height)
                {
                    HandleHeightMode(hitTile, ray, e, controlID);
                }
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                sceneView.Repaint();
            }
        }

        private void HandleAddMode(Ray ray, Event e)
        {
            if (false == _tilePrefab)
            {
                return;
            }

            Plane p = new Plane(Vector3.up, new Vector3(0, _targetY, 0));
            if (false == p.Raycast(ray, out float enter))
            {
                return;
            }

            Vector3 spawnPos = new Vector3(Mathf.Round(ray.GetPoint(enter).x), _targetY, Mathf.Round(ray.GetPoint(enter).z));
            if (e.type == EventType.Repaint)
            {
                DrawCustomGrid(spawnPos);
                Vector3 visualCenter = spawnPos + new Vector3(0.5f, 0.5f, 0.5f);

                bool isOccupied = false;
                foreach (var t in _cachedTiles)
                {
                    if (t != null && Vector3.Distance(t.transform.position, spawnPos) < 0.1f)
                    {
                        isOccupied = true; break;
                    }
                }

                Handles.color = isOccupied ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = isOccupied ? Color.red : Color.green;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (!Physics.OverlapBox(spawnPos + Vector3.one * 0.5f, Vector3.one * 0.4f).Any())
                {
                    GameObject newTile;
                    if (true == _samplingRoot)
                    {
                        newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab, _samplingRoot.transform);
                    }
                    else
                    {
                        newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab);
                        Debug.LogWarning("[Framework] Sampling Root가 지정되지 않아 타일이 씬 최상단에 생성되었습니다.");
                    }

                    newTile.transform.position = spawnPos;

                    var comp = newTile.GetComponent<EditMapTileComponent>();
                    if (true == comp)
                    {
                        comp.SetRenderLayer(_targetRenderLayer);
                        comp.SetVisualDimmed(false);
                        comp.ApplyTextures(_brushTopIndex, _brushTopAtlas, _brushSideIndex, _brushSideAtlas);
                        _cachedTiles.Add(comp);
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
                Handles.color = isEyedropper ? new Color(1f, 0f, 1f, 0.3f) : new Color(0, 1, 1, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = isEyedropper ? Color.magenta : Color.cyan;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            if (isEyedropper)
            {
                if (input.type == EventType.MouseDown && input.button == 0)
                {
                    _brushTopIndex = tile.TopTextureIndex;
                    _brushTopAtlas = tile.TopAtlasTexture;
                    _brushSideIndex = tile.SideTextureIndex;
                    _brushSideAtlas = tile.SideAtlasTexture;

                    if (_brushTopAtlas != null)
                    {
                        for (int i = 0; i < _atlasPages.Count; i++)
                        {
                            if (_atlasPages[i].Texture == _brushTopAtlas)
                            {
                                _selectedAtlasPageIndex = i;
                                break;
                            }
                        }
                    }

                    Repaint();
                    input.Use();
                }
                
                return;
            }

            if (input.button != 0)
            {
                return;
            }

            if (input.type == EventType.MouseDown || (input.type == EventType.MouseDrag && GUIUtility.hotControl == controlID))
            {
                GUIUtility.hotControl = controlID;
                tile.ApplyTextures(_brushTopIndex, _brushTopAtlas, _brushSideIndex, _brushSideAtlas);
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

            if (e.button != 0 || e.alt)
            {
                return;
            }

            if (e.type == EventType.MouseDown)
            {
                GUIUtility.hotControl = controlID; _cachedTiles.Remove(tile); Undo.DestroyObjectImmediate(tile.gameObject); e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
            {
                Undo.DestroyObjectImmediate(tile.gameObject); e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0; e.Use();
            }
        }

        private void HandleHeightMode(EditMapTileComponent tile, Ray ray, Event e, int controlID)
        {
            int nearIdx = -1;
            float floatMinDist = 40f;

            for (int i = 0; i < 13; i++)
            {
                Vector3 pPos = tile.transform.TransformPoint(new Vector3(PointOffsets[i].x, tile.GetPointLocalPos(i).y, PointOffsets[i].y));
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(pPos);
                float d = Vector2.Distance(guiPos, e.mousePosition);

                if (d < floatMinDist)
                {
                    floatMinDist = d;
                    nearIdx = i;
                }
            }

            if (e.type == EventType.Repaint)
            {
                DrawCustomGrid(tile.transform.position);

                if (nearIdx != -1 && _currentSelection == SelectionMode.Vertex)
                {
                    Vector3 pPos = tile.transform.TransformPoint(new Vector3(PointOffsets[nearIdx].x, tile.GetPointLocalPos(nearIdx).y, PointOffsets[nearIdx].y));
                    Handles.color = Color.yellow;
                    Handles.SphereHandleCap(0, pPos, Quaternion.identity, 0.12f, EventType.Repaint);
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && nearIdx != -1)
            {
                GUIUtility.hotControl = controlID;
                int delta = e.shift ? -1 : 1;
                Undo.RecordObject(tile, "Adjust Height");

                foreach (int idx in (_currentSelection == SelectionMode.Vertex ? new int[] { nearIdx } : Enumerable.Range(0, 13)))
                {
                    tile.ModifyHeightIndex(idx, delta);
                }

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

        private void ExecuteOptimizeMesh()
        {
            Dictionary<Vector2Int, EditMapTileComponent> tileMap = new Dictionary<Vector2Int, EditMapTileComponent>();
            foreach (var t in _cachedTiles)
            {
                if (false == t)
                {
                    continue;
                }

                Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(t.transform.position.x), Mathf.RoundToInt(t.transform.position.z));
                if (!tileMap.ContainsKey(gridPos)) tileMap.Add(gridPos, t);
            }

            foreach (var t in _cachedTiles)
            {
                if (false == t)
                {
                    continue;
                }

                Undo.RecordObject(t, "Optimize Side Mesh");
                t.OptimizeSides(tileMap);
            }
            Debug.Log($"[Framework] {_cachedTiles.Count}개 타일의 메쉬 최적화가 완료되었습니다.");
        }

        private void ExecuteBake()
        {
            var rootComponent = UnityEngine.Object.FindFirstObjectByType<Data.EditMapTileComponent>();
            if (false == rootComponent)
            {
                return;
            }

            try
            {
                Provider.EditMapSamplingRepoProvider baker = new Provider.EditMapSamplingRepoProvider();
                baker.Bake();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Framework] Bake 중 치명적 오류 발생: {e.Message}");
            }
        }
    }
}
#endif