#if UNITY_EDITOR
namespace Kompile.Map.Editor.Tools
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using Kompile.Map.Entity;
    using Kompile.Map.Editor.Provider;

    public class KompileMapEditorWindow : EditorWindow
    {
        private enum EditMode { None, Paint, Erase, Add, Height, Navi }
        private enum SelectionMode { Vertex, Face }

        private EditMode _currentMode = EditMode.None;
        private SelectionMode _currentSelection = SelectionMode.Vertex;

        private bool _isEditingEnabled = false;
        private bool _isAltPressed = false;

        private static readonly Vector2[] PointOffsets = new Vector2[] {
            new Vector2(0.0f, 0.0f), new Vector2(0.5f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(0.25f, 0.25f),
            new Vector2(0.75f, 0.25f), new Vector2(0.0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1.0f, 0.5f),
            new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.75f), new Vector2(0.0f, 1.0f), new Vector2(0.5f, 1.0f), new Vector2(1.0f, 1.0f)
        };

        private EditMapSamplingComponent _samplingRoot;
        private GameObject _tilePrefab;
        private float _targetY = 0f;
        private ushort _targetRenderLayer = 0;
        private bool _focusSelectedLayer = false;
        private EditMapTileComponent _lastHoveredTile;
        private Vector2 _mainScrollPos = Vector2.zero;

        private class AtlasPage { public string PageName; public Texture2D Texture; public int[] GlobalIndices = new int[64]; }
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

        private void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; LoadAllAtlases(); RefreshTileCache(); UpdateTilesFocusState(); }
        private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; ClearAllTilesFocusState(); }

        private void RefreshTileCache() { _cachedTiles.Clear(); foreach (var t in Object.FindObjectsByType<EditMapTileComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) _cachedTiles.Add(t); }

        private void UpdateTilesFocusState() { foreach (var tile in _cachedTiles) { if (tile) tile.SetVisualDimmed(_focusSelectedLayer && tile.RenderLayer != _targetRenderLayer); } SceneView.RepaintAll(); }

        private void ClearAllTilesFocusState() { foreach (var tile in _cachedTiles) if (tile) tile.SetVisualDimmed(false); }

        private void LoadAllAtlases()
        {
            _atlasPages.Clear();
            _selectedAtlasPageIndex = 0;
            if (!Directory.Exists(ROOT_INPUT_PATH)) return;

            foreach (var dir in Directory.GetDirectories(ROOT_INPUT_PATH))
            {
                string folderName = new DirectoryInfo(dir).Name;
                string tablePath = $"{dir}/MapTextureTable.asset";
                var textureTable = AssetDatabase.LoadAssetAtPath<Kompile.Map.Data.MapTextureTable>(tablePath);
                if (!textureTable) continue;

                var allFiles = Directory.GetFiles(dir, "*.png").Where(f => !Path.GetFileName(f).StartsWith("merged-")).ToList();
                Dictionary<int, string> validFiles = new Dictionary<int, string>();
                foreach (string file in allFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    var data = textureTable.TextureList.Find(x => x.TextureName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
                    if (data != null) validFiles[data.GlobalIndex] = file;
                }

                foreach (var group in validFiles.GroupBy(kvp => kvp.Key / 64).OrderBy(g => g.Key))
                {
                    string suffix = (group.Key == 0) ? "" : $"-{group.Key}";
                    string atlasPath = $"{dir}/merged-{folderName}{suffix}.png";
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                    if (tex)
                    {
                        AtlasPage page = new AtlasPage { PageName = $"{folderName}{suffix}", Texture = tex };
                        for (int j = 0; j < 64; j++) page.GlobalIndices[j] = -1;
                        foreach (var kvp in group) page.GlobalIndices[kvp.Key % 64] = kvp.Key;
                        _atlasPages.Add(page);
                    }
                }
            }
            if (_atlasPages.Count > 0) { _brushTopAtlas = _brushSideAtlas = _atlasPages[0].Texture; _brushTopIndex = _brushSideIndex = _atlasPages[0].GlobalIndices.FirstOrDefault(i => i != -1); }
        }

        private void OnGUI() {
            GUILayout.Label("Kompile Map 2.5D Editor", EditorStyles.boldLabel);
            if (GUILayout.Button(_isEditingEnabled ? "Editing: ON" : "Editing: OFF", GUILayout.Height(30))) { _isEditingEnabled = !_isEditingEnabled; SceneView.RepaintAll(); }
            
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            EditorGUI.BeginChangeCheck();
            _focusSelectedLayer = EditorGUILayout.ToggleLeft("🔍 Focus Target Layer", _focusSelectedLayer);
            if (EditorGUI.EndChangeCheck()) UpdateTilesFocusState();

            _currentMode = (EditMode)GUILayout.Toolbar((int)_currentMode, new string[] { "None", "Paint", "Erase", "Add", "Height", "Navi" });
            
            if (_currentMode != EditMode.None && _currentMode != EditMode.Navi) {
                _targetY = EditorGUILayout.FloatField("Target Base Y", _targetY);
                EditorGUI.BeginChangeCheck();
                _targetRenderLayer = (ushort)EditorGUILayout.IntField("Target Render Layer", _targetRenderLayer);
                if (EditorGUI.EndChangeCheck() && _focusSelectedLayer) UpdateTilesFocusState();
            }

            if (_currentMode == EditMode.Paint) DrawAtlasPaletteUI();
            else if (_currentMode == EditMode.Add) {
                _tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", _tilePrefab, typeof(GameObject), false);
                _samplingRoot = (EditMapSamplingComponent)EditorGUILayout.ObjectField("Sampling Root", _samplingRoot, typeof(EditMapSamplingComponent), true);
            }
            else if (_currentMode == EditMode.Height) _currentSelection = (SelectionMode)GUILayout.Toolbar((int)_currentSelection, new string[] { "Vertex", "Face" });

            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Optimize Visible Sides", GUILayout.Height(30))) ExecuteOptimizeMesh();
            if (GUILayout.Button("Bake Map", GUILayout.Height(40))) { ExecuteOptimizeMesh(); ExecuteBake(); }
        }

        private void DrawAtlasPaletteUI() {
            if (_atlasPages.Count == 0) return;
            string[] names = _atlasPages.Select(p => $"[{p.PageName}]").ToArray();
            _selectedAtlasPageIndex = EditorGUILayout.Popup("Select Theme", _selectedAtlasPageIndex, names);
            
            AtlasPage page = _atlasPages[_selectedAtlasPageIndex];
            float size = Mathf.Min(position.width - 30f, 300f);
            Rect rect = GUILayoutUtility.GetRect(size, size);
            GUI.DrawTexture(rect, page.Texture);

            Event e = Event.current;
            if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDown) {
                int col = Mathf.FloorToInt((e.mousePosition.x - rect.x) / (size / 8f));
                int row = Mathf.FloorToInt((e.mousePosition.y - rect.y) / (size / 8f));
                int idx = page.GlobalIndices[row * 8 + col];
                if (idx != -1) { if (e.button == 0) { _brushTopIndex = idx; _brushTopAtlas = page.Texture; } else { _brushSideIndex = idx; _brushSideAtlas = page.Texture; } e.Use(); }
            }
        }

        private void OnSceneGUI(SceneView sceneView) {
            if (!_isEditingEnabled || _currentMode == EditMode.None) return;
            Event e = Event.current; _isAltPressed = e.alt;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            EditMapTileComponent hitTile = null; float minDist = 40f;

            foreach (var tile in _cachedTiles) {
                if (!tile || (_focusSelectedLayer && tile.RenderLayer != _targetRenderLayer)) continue;
                for (int i = 0; i < 13; i++) {
                    Vector3 p = tile.transform.TransformPoint(new Vector3(PointOffsets[i].x, EditMapTileOperator.GetPointLocalY(tile, i), PointOffsets[i].y));
                    float d = Vector2.Distance(HandleUtility.WorldToGUIPoint(p), e.mousePosition);
                    if (d < minDist) { minDist = d; hitTile = tile; }
                }
            }
            if (_currentMode == EditMode.Add) HandleAddMode(ray, e);
            else if (hitTile) {
                if (_currentMode == EditMode.Paint) HandlePaintMode(hitTile, e);
                else if (_currentMode == EditMode.Erase) HandleEraseMode(hitTile, e);
                else if (_currentMode == EditMode.Height) HandleHeightMode(hitTile, ray, e);
            }
        }

        private void HandleAddMode(Ray ray, Event e) {
            Plane p = new Plane(Vector3.up, new Vector3(0, _targetY, 0));
            if (!p.Raycast(ray, out float enter) || !_tilePrefab) return;
            Vector3 spawnPos = new Vector3(Mathf.Round(ray.GetPoint(enter).x), _targetY, Mathf.Round(ray.GetPoint(enter).z));
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt) {
                GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(_tilePrefab, _samplingRoot ? _samplingRoot.transform : null);
                newTile.transform.position = spawnPos;
                var comp = newTile.GetComponent<EditMapTileComponent>();
                if (comp) { comp.SetRenderLayer(_targetRenderLayer); EditMapTileOperator.ApplyTextures(comp, _brushTopIndex, _brushTopAtlas, _brushSideIndex, _brushSideAtlas); EditMapTileOperator.RefreshMesh(comp); _cachedTiles.Add(comp); }
                Undo.RegisterCreatedObjectUndo(newTile, "Add Tile"); e.Use();
            }
        }

        private void HandlePaintMode(EditMapTileComponent tile, Event input) {
            if (input.alt && input.type == EventType.MouseDown) { _brushTopIndex = tile.TopTextureIndex; _brushTopAtlas = tile.TopAtlasTexture; _brushSideIndex = tile.SideTextureIndex; _brushSideAtlas = tile.SideAtlasTexture; Repaint(); input.Use(); return; }
            if (input.type == EventType.MouseDown || input.type == EventType.MouseDrag) { EditMapTileOperator.ApplyTextures(tile, _brushTopIndex, _brushTopAtlas, _brushSideIndex, _brushSideAtlas); input.Use(); }
        }

        private void HandleEraseMode(EditMapTileComponent tile, Event e) { if (e.type == EventType.MouseDown && e.button == 0) { _cachedTiles.Remove(tile); Undo.DestroyObjectImmediate(tile.gameObject); e.Use(); } }

        private void HandleHeightMode(EditMapTileComponent tile, Ray ray, Event e) {
            int nearIdx = -1; float dMin = 40f;
            for (int i = 0; i < 13; i++) {
                float d = Vector2.Distance(HandleUtility.WorldToGUIPoint(tile.transform.TransformPoint(new Vector3(PointOffsets[i].x, EditMapTileOperator.GetPointLocalY(tile, i), PointOffsets[i].y))), e.mousePosition);
                if (d < dMin) { dMin = d; nearIdx = i; }
            }
            if (e.type == EventType.MouseDown && e.button == 0 && nearIdx != -1) {
                int delta = e.shift ? -1 : 1;
                if (_currentSelection == SelectionMode.Vertex) EditMapTileOperator.ModifyHeightIndex(tile, nearIdx, delta);
                else for (int i = 0; i < 13; i++) EditMapTileOperator.ModifyHeightIndex(tile, i, delta);
                e.Use();
            }
        }

        private void ExecuteOptimizeMesh() {
            var tileMap = _cachedTiles.Where(t => t).ToDictionary(t => new Vector2Int(Mathf.RoundToInt(t.transform.position.x), Mathf.RoundToInt(t.transform.position.z)), t => t);
            foreach (var t in _cachedTiles) if (t) EditMapTileOperator.OptimizeSides(t, tileMap);
        }

        private void ExecuteBake() { new EditMapSamplingProvider().Bake(); }
    }
}
#endif