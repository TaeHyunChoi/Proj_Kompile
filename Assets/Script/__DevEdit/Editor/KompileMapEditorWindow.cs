#if UNITY_EDITOR
namespace Kompile.Editor.Tools
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using System.Collections.Generic;
    using Kompile.Data;
    using Data;
    using Domain;
    using Entities;
    using UnityEditor.SceneManagement;
    using Kompile.Editor.Utility;

    /// <summary>
    /// Editor Manager: 멀티 아틀라스 팔레트, 스포이드, Focus Mode를 지원하는 통합 맵 에디터.
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

        [SerializeField] private EditMapSamplingComponent _samplingRoot;
        [SerializeField] private GameObject _tilePrefab;
        
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
            Undo.undoRedoPerformed += OnUndoRedo;
            
            if (!_samplingRoot)
            {
                _samplingRoot = UnityEngine.Object.FindFirstObjectByType<EditMapSamplingComponent>();
            }
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo; // [추가] 이벤트 감지 해제
            
            ClearAllTilesFocusState();
        }

        /// <summary> Ctrl+Z(실행 취소) 또는 Ctrl+Y(다시 실행)가 눌렸을 때 자동으로 호출 </summary>
        private void OnUndoRedo()
        {
            // Erase로 지웠던 타일이 살아났거나 Add로 추가한 타일이 사라졌을 수 있으므로 캐시 목록을 다시 가져옵니다.
            RefreshTileCache();

            // 복구된 과거의 데이터를 바탕으로 씬 뷰의 메쉬와 텍스처를 강제로 다시 갱신합니다.
            foreach (EditMapTileComponent tile in _cachedTiles)
            {
                if (tile)
                {
                    EditMapTileOperator.RefreshMesh(tile);
                    tile.UpdateMaterialProperties();
                }
            }

            // 포커스 레이어 상태를 맞추고 씬 뷰를 즉시 다시 그립니다.
            UpdateTilesFocusState();
            SceneView.RepaintAll();
            Repaint();
        }
        private void LoadAllAtlases()
        {
            _atlasPages.Clear();
            _selectedAtlasPageIndex = 0;

            if (!Directory.Exists(ROOT_INPUT_PATH))
            {
                return;
            }

            string[] directories = Directory.GetDirectories(ROOT_INPUT_PATH);
            foreach (string dir in directories)
            {
                string folderName = new DirectoryInfo(dir).Name;
                string tablePath = $"{dir}/MapTextureTable.asset";
                
                MapTextureTable textureTable = AssetDatabase.LoadAssetAtPath<MapTextureTable>(tablePath);
                if (!textureTable)
                {
                    Debug.LogWarning($"[Framework] {folderName} 폴더에 MapTextureTable 에셋이 없습니다. 병합기를 먼저 실행해주세요.");
                    continue;
                }

                string[] files = Directory.GetFiles(dir, "*.png");
                List<string> allFiles = new List<string>(files.Length);
                foreach (string file in files)
                {
                    if (!Path.GetFileName(file).StartsWith("merged-"))
                    {
                        allFiles.Add(file);
                    }
                }

                Dictionary<int, string> validFiles = new Dictionary<int, string>();
                foreach (string file in allFiles)
                {
                    string fileNameNoExt = Path.GetFileNameWithoutExtension(file);
                    
                    for (int i = 0; i < textureTable.TextureList.Count; i++)
                    {
                        MapTextureData textureData = textureTable.TextureList[i];
                        if (textureData.TextureName.Equals(fileNameNoExt, System.StringComparison.OrdinalIgnoreCase))
                        {
                            validFiles[textureData.GlobalIndex] = file;
                            break; // Find()와 동일하게 첫 번째로 조건을 만족하면 탐색 종료
                        }
                    }
                }

                List<FileGroup> groupedFiles = GetGroupFiles(validFiles);
                for (int i = 0; i < groupedFiles.Count; i++)
                {
                    FileGroup group = groupedFiles[i];
    
                    // IGrouping.Key 대신 FileGroup.GroupId를 참조
                    int atlasPageNum = group.GroupId;
                    string suffix = (atlasPageNum == 0) ? "" : $"-{atlasPageNum}";
                    string atlasPath = $"{dir}/merged-{folderName}{suffix}.png";

                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                    if (tex) 
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

                        // IGrouping 자체를 순회하던 것을 FileGroup.Files 리스트 순회로 변경
                        List<KeyValuePair<int, string>> groupFiles = group.Files;
                        for (int k = 0; k < groupFiles.Count; k++)
                        {
                            var kvp = groupFiles[k];
            
                            // 최적화: % 64 대신 비트 연산(& 63) 사용 (64는 2의 6승이므로 63(00111111)과 AND 연산하면 나머지 값과 동일)
                            int localIndex = kvp.Key & 0b_0011_1111; 
                            page.GlobalIndices[localIndex] = kvp.Key;
                        }
        
                        _atlasPages.Add(page);
                    }
                }
            }

            if (_atlasPages.Count > 0)
            {
                if (!_brushTopAtlas)
                {
                    _brushTopAtlas = _atlasPages[0].Texture;
                    _brushTopIndex = 0;
                    for (int fi = 0; fi < _atlasPages[0].GlobalIndices.Length; fi++)
                    {
                        if (_atlasPages[0].GlobalIndices[fi] != -1)
                        {
                            _brushTopIndex = _atlasPages[0].GlobalIndices[fi];
                            break;
                        }
                    }
                }

                if (!_brushSideAtlas)
                {
                    _brushSideAtlas = _atlasPages[0].Texture;
                    _brushSideIndex = 0;
                    for (int fi = 0; fi < _atlasPages[0].GlobalIndices.Length; fi++)
                    {
                        if (_atlasPages[0].GlobalIndices[fi] != -1)
                        {
                            _brushSideIndex = _atlasPages[0].GlobalIndices[fi];
                            break;
                        }
                    }
                }
            }
        }

        // LINQ의 IGrouping<TKey, TElement>를 대체할 가벼운 구조체 정의
        public struct FileGroup
        {
            public int GroupId;
            public List<KeyValuePair<int, string>> Files;
        }
        private List<FileGroup> GetGroupFiles(Dictionary<int, string> validFiles)
        {
            // 1. 그룹핑 (버킷팅) 과정 - GroupBy 대체
            Dictionary<int, List<KeyValuePair<int, string>>> buckets = new Dictionary<int, List<KeyValuePair<int, string>>>();
            foreach (KeyValuePair<int, string> kvp in validFiles)
            {
                // 최적화: / 64 대신 비트 시프트 연산(>> 6) 사용
                int groupId = kvp.Key >> 6;

                if (!buckets.TryGetValue(groupId, out List<KeyValuePair<int, string>> list))
                {
                    list = new List<KeyValuePair<int, string>>();
                    buckets.Add(groupId, list);
                }

                list.Add(kvp);
            }

            // 2. 키 추출 및 정렬 - OrderBy 대체
            List<int> sortedGroupIds = new List<int>(buckets.Keys);
            sortedGroupIds.Sort(); // LINQ 없이 내부 배열에서 직접 정렬 (추가 GC 할당 없음)

            // 3. 최종 리스트 생성 - ToList 대체
            List<FileGroup> groupedFiles = new List<FileGroup>(sortedGroupIds.Count);
            for (int i = 0; i < sortedGroupIds.Count; i++)
            {
                int id = sortedGroupIds[i];
                groupedFiles.Add(new FileGroup { GroupId = id, Files = buckets[id] });
            }

            return groupedFiles;
        }

        private void RefreshTileCache()
        {
            _cachedTiles.Clear();
            
            EditMapTileComponent[] allTiles = FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (EditMapTileComponent t in allTiles)
            {
                _cachedTiles.Add(t);                
            }
        }

        private void UpdateTilesFocusState()
        {
            foreach (EditMapTileComponent tile in _cachedTiles)
            {
                if (tile)
                {
                    bool dim = _focusSelectedLayer && (tile.RenderLayer != _targetRenderLayer);
                    tile.SetVisualDimmed(dim);                    
                }
            }
            
            SceneView.RepaintAll();
        }

        private void ClearAllTilesFocusState()
        {
            foreach (EditMapTileComponent tile in _cachedTiles)
            {
                if (tile)
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
                case EditMode.Height: 
                    _currentSelection = (SelectionMode)GUILayout.Toolbar((int)_currentSelection, new string[] { "Vertex", "Face" }); 
                    break;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Optimize Visible Sides (옆면 최적화)", GUILayout.Height(30)))
            {
                ExecuteOptimizeMesh();
            }

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

            string[] pageNames = new string[_atlasPages.Count];
            for (int i = 0; i < _atlasPages.Count; i++)
            {
                pageNames[i] = $"[{_atlasPages[i].PageName}] Atlas";
            }
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
            float cellSize = atlasSize * 0.125f; // == / 8
            int hoveredIndex = -1;
            int hoveredGlobalIndex = -1;

            if (atlasRect.Contains(e.mousePosition))
            {
                float cellSize_recip = 1 / cellSize;
                
                int col = Mathf.FloorToInt((e.mousePosition.x - atlasRect.x) * cellSize_recip);
                int row = Mathf.FloorToInt((e.mousePosition.y - atlasRect.y) * cellSize_recip);
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
                        EditorGUI.DrawRect(
                            new Rect(cellRect.x, cellRect.y + cellSize * 0.5f, cellSize, cellSize * 0.5f),
                            new Color(0, 0.5f, 1, 0.4f));
                        GUI.Label(new Rect(cellRect.x, cellRect.y + cellSize - 15, cellSize, 15), " S",
                            EditorStyles.whiteMiniLabel);
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
            rect.x += (80 - 64) * 0.5f;
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

            if (atlas is not null)
            {
                // 정수 나눗셈과 나머지 연산을 비트 연산으로 교체
                int localIndex = index & 63;  // index % 64
                int col = localIndex & 7;     // localIndex % 8
                int row = localIndex >> 3;    // localIndex / 8

                const float CellSize = 0.125f; // 1 / 8 = 0.125
                float uvX = col * CellSize;
                float uvY = 1f - ((row + 1) * CellSize);

                Rect uvRect = new Rect(uvX, uvY, CellSize, CellSize);
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
                sceneView.Repaint(); 
                Repaint();
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
                    if (!tile)
                    {
                        continue;
                    }

                    if (_focusSelectedLayer && tile.RenderLayer != _targetRenderLayer)
                    {
                        continue;
                    }

                    if (Mathf.Abs(tile.transform.position.y - _targetY) > 0.1f)
                    {
                        continue;
                    }

                    for (int i = 0; i < 13; i++)
                    {
                        float localY = EditMapTileOperator.GetPointLocalY(tile, i);
                        Vector3 pointPos = tile.transform.TransformPoint(new Vector3(PointOffsets[i].x, localY, PointOffsets[i].y));
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
            else if (hitTile)
            {
                switch (_currentMode)
                {
                    case EditMode.Paint:
                        HandlePaintMode(hitTile, e, controlID);
                        break;
                    case EditMode.Erase:
                        HandleEraseMode(hitTile, e, controlID);
                        break;
                    case EditMode.Height:
                        HandleHeightMode(hitTile, ray, e, controlID);
                        break;
                }
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
            {
                sceneView.Repaint();
            }
        }
        private void HandleAddMode(Ray ray, Event e)
        {
            if (!_tilePrefab)
            {
                return;
            }

            Plane p = new Plane(Vector3.up, new Vector3(0, _targetY, 0));
            if (!p.Raycast(ray, out float enter))
            {
                return;
            }

            Vector3 spawnPos = new Vector3(Mathf.Round(ray.GetPoint(enter).x), _targetY, Mathf.Round(ray.GetPoint(enter).z));
            if (e.type == EventType.Repaint)
            {
                DrawCustomGrid(spawnPos);
                Vector3 visualCenter = spawnPos + new Vector3(0.5f, 0.5f, 0.5f);

                bool isOccupied = false;
                foreach (EditMapTileComponent t in _cachedTiles)
                {
                    if (t && Vector3.Distance(t.transform.position, spawnPos) < 0.1f)
                    {
                        isOccupied = true; 
                        break;
                    }
                }

                Handles.color = isOccupied ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = isOccupied ? Color.red : Color.green;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (Physics.OverlapBox(spawnPos + Vector3.one * 0.5f, Vector3.one * 0.4f).Length == 0)
                {
                    GameObject newTile;
                    if (_samplingRoot)
                    {
                        newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab, _samplingRoot.transform);
                    }
                    else
                    {
                        newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab);
                        Debug.LogWarning("[Framework] Sampling Root가 지정되지 않아 타일이 씬 최상단에 생성되었습니다.");
                    }

                    newTile.transform.position = spawnPos;

                    EditMapTileComponent comp = newTile.GetComponent<EditMapTileComponent>();
                    if (comp)
                    {
                        comp.SetRenderLayer(_targetRenderLayer);
                        comp.SetVisualDimmed(false);
                        
                        EditMapTileOperator.ApplyTextures(comp, _brushTopIndex, _brushTopAtlas, _brushSideIndex, _brushSideAtlas);
                        EditMapTileOperator.RefreshMesh(comp);
                        
                        _cachedTiles.Add(comp);
                    }

                    Undo.RegisterCreatedObjectUndo(newTile, "Add Tile");
                    e.Use();
                    SceneView.RepaintAll();
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

                    if (_brushTopAtlas)
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
                
                EditMapTileOperator.ApplyTextures(tile, _brushTopIndex, _brushTopAtlas, _brushSideIndex, _brushSideAtlas);
                input.Use();
                SceneView.RepaintAll(); // [추가]
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

            switch (e.type)
            {
                case EventType.MouseDown:
                    GUIUtility.hotControl = controlID; 
                    _cachedTiles.Remove(tile); 
                    Undo.DestroyObjectImmediate(tile.gameObject); 
                    e.Use();
                    SceneView.RepaintAll();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == controlID:
                    _cachedTiles.Remove(tile); 
                    Undo.DestroyObjectImmediate(tile.gameObject); 
                    e.Use();
                    SceneView.RepaintAll();
                    break;
                case EventType.MouseUp:
                    GUIUtility.hotControl = 0; 
                    e.Use();
                    break;
            }
        }

private void HandleHeightMode(EditMapTileComponent tile, Ray ray, Event e, int controlID)
        {
            int nearIdx = -1;
            float floatMinDist = 40f;

            for (int i = 0; i < 13; i++)
            {
                float localY = EditMapTileOperator.GetPointLocalY(tile, i);
                Vector3 pPos = tile.transform.TransformPoint(new Vector3(PointOffsets[i].x, localY, PointOffsets[i].y));
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(pPos);
                float d = Vector2.Distance(guiPos, e.mousePosition);

                if (d < floatMinDist)
                {
                    floatMinDist = d;
                    nearIdx = i;
                }
            }

            switch (e.type)
            {
                case EventType.Repaint:
                    {
                        DrawCustomGrid(tile.transform.position);

                        if (nearIdx != -1 && _currentSelection == SelectionMode.Vertex)
                        {
                            float localY = EditMapTileOperator.GetPointLocalY(tile, nearIdx);
                            Vector3 pPos = tile.transform.TransformPoint(new Vector3(PointOffsets[nearIdx].x, localY, PointOffsets[nearIdx].y));
                    
                            Handles.color = Color.yellow;
                            Handles.SphereHandleCap(0, pPos, Quaternion.identity, 0.12f, EventType.Repaint);
                        }

                        break;
                    }
                case EventType.MouseDown when e.button == 0 && !e.alt && nearIdx != -1:
                    {
                        GUIUtility.hotControl = controlID;
                        int delta = e.shift ? -1 : 1;
                
                        // [핵심 해결] Face 모드일 때 13개의 정점을 한 번의 Undo로 되돌리기 위해 액션을 그룹화합니다.
                        Undo.SetCurrentGroupName("Adjust Height");
                        int undoGroup = Undo.GetCurrentGroup();

                        if (_currentSelection == SelectionMode.Vertex)
                        {
                            EditMapTileOperator.ModifyHeightIndex(tile, nearIdx, delta);
                        }
                        else
                        {
                            for (int idx = 0; idx < 13; idx++)
                            {
                                EditMapTileOperator.ModifyHeightIndex(tile, idx, delta);
                            }
                        }
                
                        // 생성된 여러 개의 Undo 액션을 하나로 병합!
                        Undo.CollapseUndoOperations(undoGroup);
                
                        e.Use();
                        SceneView.RepaintAll();
                        break;
                    }
                case EventType.MouseUp:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
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
            foreach (EditMapTileComponent t in _cachedTiles)
            {
                if (false == t)
                {
                    continue;
                }

                Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(t.transform.position.x), Mathf.RoundToInt(t.transform.position.z));
                tileMap.TryAdd(gridPos, t);
            }

            foreach (EditMapTileComponent t in _cachedTiles)
            {
                if (!t)
                {
                    continue;
                }

                Undo.RecordObject(t, "Optimize Side Mesh");
                EditMapTileOperator.OptimizeSides(t, tileMap);
            }
            Debug.Log($"[Framework] {_cachedTiles.Count}개 타일의 메쉬 최적화가 완료되었습니다.");
            SceneView.RepaintAll();
        }

        private void ExecuteBake()
        {
            EditMapSamplingComponent rootComponent = UnityEngine.Object.FindFirstObjectByType<EditMapSamplingComponent>();
            if (!rootComponent)
            {
                return;
            }

            try
            {
                EditMapSamplingProvider baker = new EditMapSamplingProvider();
                baker.Bake();
                
                if (EditorSceneManager.SaveOpenScenes())
                {
                    Debug.Log("나으리, 모든 활성 씬이 무사히 저장되었습니다.");
                }
                else
                {
                    Debug.LogWarning("저장 중에 문제가 발생했습니다.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Framework] Bake 중 치명적 오류 발생: {e.Message}");
            }
        }
    }
}
#endif