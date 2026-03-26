#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Script.Index;
    using Script.Map.Data;
    using Script.Map.Entity;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// [Framework] Editor Manager: 아틀라스 기반 팔레트, 스포이드, Focus Mode를 지원하는 통합 맵 에디터입니다.
    /// </summary>
    public class KompileMapEditorWindow : EditorWindow
    {
        private enum EditMode { None, Paint, Erase, Add, Height, Navi }
        private enum SelectionMode { Vertex, Face }

        // [신규] 브러시 타겟 모드 (아틀라스에서 클릭한 텍스처를 어디에 할당할 것인가)
        private enum BrushTarget { TopTexture, SideTexture }

        private EditMode _currentMode = EditMode.None;
        private SelectionMode _currentSelection = SelectionMode.Vertex;
        private BrushTarget _brushTarget = BrushTarget.TopTexture;

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

        private GameObject _tilePrefab;
        private float _targetY = 0f;
        private ushort _targetRenderLayer = 0;
        private bool _focusSelectedLayer = false;

        private EditMapTileComponent _lastHoveredTile;

        // [신규] 아틀라스 텍스처 및 즉석 브러시 데이터
        private Texture2D _atlasTexture;
        private const string ATLAS_PATH = "Assets/Editor/EditTexture/MergedTexture.png";

        // 브러시가 들고 있는 현재 텍스처 인덱스 (기본값 0)
        private int _brushTopIndex = 0;
        private int _brushSideIndex = 0;

        // Manager-Centric 캐싱 로직 부활
        private HashSet<EditMapTileComponent> _cachedTiles = new HashSet<EditMapTileComponent>();

        private Vector2 _mainScrollPos = Vector2.zero;

        [MenuItem("Tools/Map/Map Editor")]
        public static void ShowWindow() => GetWindow<KompileMapEditorWindow>("Kompile Map Editor").Show();

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            LoadAtlasTexture(); // 아틀라스 로드
            RefreshTileCache(); // 캐싱
            UpdateTilesFocusState();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            ClearAllTilesFocusState();
        }

        private void LoadAtlasTexture()
        {
            _atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ATLAS_PATH);
            if (null == _atlasTexture)
            {
                Debug.LogWarning($"[Framework] 아틀라스 텍스처를 찾을 수 없습니다: {ATLAS_PATH}");
            }
        }

        private void RefreshTileCache()
        {
            _cachedTiles.Clear();
            var allTiles = Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var t in allTiles) _cachedTiles.Add(t);
        }

        private void UpdateTilesFocusState()
        {
            foreach (EditMapTileComponent tile in _cachedTiles)
            {
                if (tile != null) tile.SetVisualDimmed(true == _focusSelectedLayer && (tile.RenderLayer != _targetRenderLayer));
            }
            SceneView.RepaintAll();
        }

        private void ClearAllTilesFocusState()
        {
            foreach (var tile in _cachedTiles)
            {
                if (tile != null) tile.SetVisualDimmed(false);
            }
        }

        private void OnGUI()
        {
            // ==========================================
            // 1. 고정 헤더 영역 (항상 상단에 표시)
            // ==========================================
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

            // ==========================================
            // 2. 스크롤 본문 영역 (내용이 길어지면 스크롤 생성)
            // ==========================================
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("View Options", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _focusSelectedLayer = EditorGUILayout.ToggleLeft("🔍 Focus Target Layer (해당 레이어 외 어둡게 표시)", _focusSelectedLayer);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateTilesFocusState();
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
                    if (_focusSelectedLayer) UpdateTilesFocusState();
                }

                EditorGUILayout.HelpBox($"현재 {_targetY}층 / 타겟 렌더 레이어 {_targetRenderLayer} 에서 작업 중입니다.", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            switch (_currentMode)
            {
                case EditMode.Paint: DrawAtlasPaletteUI(); break;
                case EditMode.Add: _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", _tilePrefab, typeof(GameObject), false); break;
                case EditMode.Height: _currentSelection = (SelectionMode)GUILayout.Toolbar((int)_currentSelection, new string[] { "Vertex", "Face" }); break;
            }

            EditorGUILayout.Space(10); // 스크롤 끝부분 여백
            EditorGUILayout.EndScrollView();
            // ==========================================

            // ==========================================
            // 3. 고정 푸터 영역 (항상 하단에 고정)
            // ==========================================
            // FlexibleSpace()를 통해 버튼들을 창의 맨 아래로 밀어냅니다.
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Optimize Visible Sides (옆면 최적화)", GUILayout.Height(30)))
            {
                ExecuteOptimizeMesh();
            }

            if (GUILayout.Button("Bake Map (Combine Meshes)", GUILayout.Height(40)))
            {
                ExecuteOptimizeMesh();
                if (EditorUtility.DisplayDialog("Bake Map", "맵 데이터를 구우시겠습니까?", "Bake", "Cancel")) ExecuteBake();
            }
        }

        // ==============================================================================
        // [핵심 변경부] 다이렉트 아틀라스 브러시 뷰어 (탭 버튼 제거, 좌/우클릭 완전 분리)
        // ==============================================================================
        private void DrawAtlasPaletteUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Direct Atlas Brush", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 리로드", GUILayout.Width(80))) { LoadAtlasTexture(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_atlasTexture == null)
            {
                EditorGUILayout.HelpBox($"아틀라스 이미지를 찾을 수 없습니다.\n경로: {ATLAS_PATH}", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // [신규] 탭 버튼 삭제 및 직관적인 조작법 안내
            EditorGUILayout.HelpBox(
                "💡 [아틀라스 픽업 조작법]\n" +
                "• 좌클릭 (Left Click) : 윗면(Top) 텍스처 지정\n" +
                "• 우클릭 (Right Click) : 옆면(Side) 텍스처 지정",
                MessageType.Info);
            EditorGUILayout.Space();

            // 아틀라스 렌더링
            float atlasSize = Mathf.Min(position.width - 30f, 300f);
            Rect atlasRect = GUILayoutUtility.GetRect(atlasSize, atlasSize, GUILayout.ExpandWidth(false));
            atlasRect.x += (position.width - atlasSize - 20f) / 2f;

            GUI.DrawTexture(atlasRect, _atlasTexture, ScaleMode.ScaleToFit);

            Event e = Event.current;
            float cellSize = atlasSize / 8f;
            int hoveredIndex = -1;

            if (atlasRect.Contains(e.mousePosition))
            {
                int col = Mathf.FloorToInt((e.mousePosition.x - atlasRect.x) / cellSize);
                int row = Mathf.FloorToInt((e.mousePosition.y - atlasRect.y) / cellSize);
                hoveredIndex = row * 8 + col;

                // [핵심] 마우스 클릭 완벽 분리
                if (e.type == EventType.MouseDown)
                {
                    if (e.button == 0) // 무조건 좌클릭은 Top
                    {
                        _brushTopIndex = hoveredIndex;
                        e.Use(); // 이벤트 소모
                    }
                    else if (e.button == 1) // 무조건 우클릭은 Side
                    {
                        _brushSideIndex = hoveredIndex;
                        e.Use(); // 이벤트 소모
                    }
                }
            }

            // 그리드 및 선택 박스 그리기
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Rect cellRect = new Rect(atlasRect.x + c * cellSize, atlasRect.y + r * cellSize, cellSize, cellSize);
                    int currentIndex = r * 8 + c;

                    // Top 텍스처 위치 표시 (초록색)
                    if (currentIndex == _brushTopIndex)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(0, 1, 0, 0.4f));
                        Handles.color = Color.green;
                        Handles.DrawWireCube(cellRect.center, new Vector3(cellSize, cellSize, 0));
                        GUI.Label(new Rect(cellRect.x, cellRect.y, cellSize, 15), " T", EditorStyles.whiteMiniLabel);
                    }
                    // Side 텍스처 위치 표시 (파란색) - Top과 같을 수 있음
                    if (currentIndex == _brushSideIndex)
                    {
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellSize / 2, cellSize, cellSize / 2), new Color(0, 0.5f, 1, 0.4f));
                        Handles.color = Color.blue;
                        Handles.DrawWireCube(cellRect.center, new Vector3(cellSize - 2, cellSize - 2, 0));
                        GUI.Label(new Rect(cellRect.x, cellRect.y + cellSize - 15, cellSize, 15), " S", EditorStyles.whiteMiniLabel);
                    }

                    if (currentIndex == hoveredIndex)
                    {
                        EditorGUI.DrawRect(cellRect, new Color(1, 1, 1, 0.2f));
                    }

                    Handles.color = new Color(1, 1, 1, 0.1f);
                    Handles.DrawWireCube(cellRect.center, new Vector3(cellSize, cellSize, 0));
                }
            }

            EditorGUILayout.Space();

            // 브러시 상태 정보 표시
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"🖌️ Current Brush Settings", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Top Texture: Index {_brushTopIndex} ({(Script.Index.MapTextureType)_brushTopIndex})");
            EditorGUILayout.LabelField($"Side Texture: Index {_brushSideIndex} ({(Script.Index.MapTextureType)_brushSideIndex})");
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

                if (hitTile == null)
                {
                    Plane gridPlane = new Plane(Vector3.up, new Vector3(0, _targetY, 0));
                    if (gridPlane.Raycast(ray, out float enter))
                    {
                        Vector3 hitPos = ray.GetPoint(enter);
                        foreach (var t in _cachedTiles)
                        {
                            if (t == null || Mathf.Abs(t.transform.position.y - _targetY) > 0.1f) continue;

                            Vector3 tPos = t.transform.position;
                            if (hitPos.x >= tPos.x && hitPos.x <= tPos.x + 1f &&
                                hitPos.z >= tPos.z && hitPos.z <= tPos.z + 1f)
                            {
                                hitTile = t;
                                break;
                            }
                        }
                    }
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

                bool isOccupied = false;
                foreach (var t in _cachedTiles)
                {
                    if (t != null && Vector3.Distance(t.transform.position, spawnPos) < 0.1f) { isOccupied = true; break; }
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
                    GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab);
                    newTile.transform.position = spawnPos;

                    var comp = newTile.GetComponent<EditMapTileComponent>();
                    if (comp != null)
                    {
                        comp.SetRenderLayer(_targetRenderLayer);
                        comp.SetVisualDimmed(false);
                        _cachedTiles.Add(comp); // 매니저 명부 등록
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
                Color handleCapColor;
                Color drawCubeColor;

                if (isEyedropper)
                {
                    handleCapColor = new Color(1f, 0f, 1f, 0.3f); ;
                    drawCubeColor = Color.magenta;

                    GUIStyle labelStyle = new GUIStyle(EditorStyles.helpBox);
                    labelStyle.fontStyle = FontStyle.Bold;
                    labelStyle.normal.textColor = Color.white; 
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    
                    Handles.Label(tile.transform.position + new Vector3(0.5f, 1.2f, 0.5f), "🎨 스포이드", labelStyle);
                }
                else
                {
                    handleCapColor = new Color(0, 1, 1, 0.3f);
                    drawCubeColor = Color.cyan;
                }

                Handles.color = handleCapColor;
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = drawCubeColor;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            if (true == isEyedropper)
            {
                if (input.type == EventType.MouseDown && input.button == 0)
                {
                    // 스포이드 시 에셋을 가져오는게 아니라 타일의 인덱스 값을 브러시에 저장
                    _brushTopIndex = tile.TopTextureIndex;
                    _brushSideIndex = tile.SideTextureIndex;
                    Repaint();
                    input.Use();
                }

                return;
            }
            if (input.button != 0)
            {
                return;
            }

            if (input.type == EventType.MouseDown 
                || (input.type == EventType.MouseDrag && GUIUtility.hotControl == controlID))
            {
                GUIUtility.hotControl = controlID;
                tile.ApplyTextures(_brushTopIndex, _brushSideIndex);
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

            if (e.type == EventType.MouseDown) { GUIUtility.hotControl = controlID; _cachedTiles.Remove(tile); Undo.DestroyObjectImmediate(tile.gameObject); e.Use(); }
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

                foreach (int idx in (_currentSelection == SelectionMode.Vertex ? new int[] { nearIdx } : Enumerable.Range(0, 13)))
                {
                    sbyte currentHeight = tile.GetHeightData(idx);
                    Handles.color = (currentHeight == -1) ? Color.red : Color.yellow;

                    Vector3 pPos = new Vector3(PointOffsets[idx].x, tile.GetPointLocalPos(idx).y, PointOffsets[idx].y);
                    Handles.SphereHandleCap(0, tile.transform.TransformPoint(pPos), Quaternion.identity, 0.08f, EventType.Repaint);
                }
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
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

        private void ExecuteBake()
        {
            // 1. 씬에 맵 최상위 루트(EditMapSamplingComponent)가 있는지 안전 검사
            var rootComponent = UnityEngine.Object.FindFirstObjectByType<EditMapSamplingComponent>();
            if (rootComponent == null)
            {
                EditorUtility.DisplayDialog("Bake Error",
                    "씬에 EditMapSamplingComponent가 존재하지 않습니다.\n맵 타일들을 묶어줄 최상위 루트 오브젝트를 생성해주세요.",
                    "확인");
                return;
            }

            try
            {
                // 2. Provider 인스턴스를 생성하고 굽기 실행 (Value & Asset-Centric 처리)
                Script.Map.Provider.EditMapSamplingRepoProvider baker = new Script.Map.Provider.EditMapSamplingRepoProvider();
                baker.Bake();

                // 3. 완료 알림
                EditorUtility.DisplayDialog("Bake Complete",
                    "맵 데이터와 메쉬가 성공적으로 병합되어 저장되었습니다!",
                    "확인");
            }
            catch (System.Exception e)
            {
                // 굽기 중 에러 발생 시 콘솔과 팝업으로 상세 안내
                Debug.LogError($"[Framework] Bake 중 치명적 오류 발생: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Bake Failed",
                    $"굽기 작업 중 오류가 발생했습니다. 콘솔 창을 확인해주세요.\n\n{e.Message}",
                    "확인");
            }
            finally
            {
                // 진행바가 켜진 상태로 에러가 났을 경우를 대비한 안전장치
                EditorUtility.ClearProgressBar();
            }
        }

        private void ExecuteOptimizeMesh()
        {
            Dictionary<Vector2Int, EditMapTileComponent> tileMap = new Dictionary<Vector2Int, EditMapTileComponent>();
            foreach (var t in _cachedTiles)
            {
                if (t == null) continue;
                Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(t.transform.position.x), Mathf.RoundToInt(t.transform.position.z));
                if (!tileMap.ContainsKey(gridPos)) tileMap.Add(gridPos, t);
            }

            foreach (var t in _cachedTiles)
            {
                if (t == null) continue;
                Undo.RecordObject(t, "Optimize Side Mesh");
                t.OptimizeSides(tileMap);
            }

            Debug.Log($"[Framework] {_cachedTiles.Count}개 타일의 메쉬 최적화가 완료되었습니다.");
        }
    }
}
#endif