#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Script.Index;
    using Script.Map.Data;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// [Framework] Editor Manager: 모든 편집 모드에서 Y축 층 제한을 시각화하고 제어합니다.
    /// </summary>
    public class KompileMapEditorWindow : EditorWindow
    {
        // --- 에디터 상태 변수 ---
        private enum EditMode { None, Paint, Erase, Add, Height, Navi }
        private enum SelectionMode { Vertex, Face }

        private EditMode _currentMode = EditMode.None;
        private MapTextureType _selectedTexture = MapTextureType.map_w;
        private SelectionMode _currentSelection = SelectionMode.Vertex;

        private bool _isEditingEnabled = false;

        private EditMapTileComponent _lastHoveredTile;

        // 편집 기준 높이 변수
        private GameObject _tilePrefab;
        private float _targetY = 0f;

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
            window.minSize = new Vector2(350, 450);
            window.Show();
        }

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
            _currentMode = (EditMode)GUILayout.Toolbar((int)_currentMode, new string[] { "None", "Paint", "Erase", "Add", "Height", "Navi" });
            EditorGUILayout.Space();

            // 3. [개선] 공통 설정 레이아웃 (편집 모드일 때 항상 표시)
            if (_currentMode != EditMode.None && _currentMode != EditMode.Navi)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("Layer Settings", EditorStyles.boldLabel);

                // 모든 모드에서 Y값을 바로 수정할 수 있게 배치
                _targetY = EditorGUILayout.FloatField("Target Base Y (제한 층)", _targetY);

                // 나으리께서 요청하신 안내 메시지 표기
                EditorGUILayout.HelpBox($"현재 {_targetY}층에서만 작업이 가능합니다.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            // 4. 모드별 특화 설정 UI
            switch (_currentMode)
            {
                case EditMode.Paint:
                    _selectedTexture = (MapTextureType)EditorGUILayout.EnumPopup("Brush Texture", _selectedTexture);
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

        private void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; }
        private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEditingEnabled || _currentMode == EditMode.None) return;

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID("KompileMapEditor".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(controlID);

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            EditMapTileComponent hitTile = null;

            // --- 픽킹 로직: 설정된 _targetY 층에 있는 타일만 골라냅니다 ---
            if (e.type != EventType.Layout && e.type != EventType.Repaint)
            {
                GameObject pickedObj = HandleUtility.PickGameObject(e.mousePosition, false);
                if (pickedObj != null)
                {
                    var foundTile = pickedObj.GetComponentInParent<EditMapTileComponent>();
                    // Y값 일치 여부 검사 (오차 범위 허용)
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

            // 모드별 분기 처리
            if (_currentMode == EditMode.Add)
            {
                HandleAddMode(ray, e);
            }
            else if (hitTile != null)
            {
                if (_currentMode == EditMode.Paint) HandlePaintMode(hitTile, e);
                else if (_currentMode == EditMode.Erase) HandleEraseMode(hitTile, e);
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

        private void HandlePaintMode(EditMapTileComponent tile, Event input)
        {
            // 픽킹 단계에서 이미 층 필터링이 끝났으므로 칠하기만 수행
            if (input.type == EventType.Repaint)
            {
                Vector3 visualCenter = tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f);
                Handles.color = new Color(0, 1, 1, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = Color.cyan;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            if (0 != input.button) return;
            if (!(input.type == EventType.MouseDown || input.type == EventType.MouseDrag)) return;
            if ((int)_selectedTexture == tile.TextureIndex) return;

            Undo.RecordObject(tile, "Paint Tile Texture");
            SerializedObject so = new SerializedObject(tile);
            SerializedProperty texProp = so.FindProperty("textureType");
            if (texProp != null)
            {
                texProp.enumValueIndex = (int)_selectedTexture;
                so.ApplyModifiedProperties();
            }
            tile.UpdateMesh();
            input.Use();
        }

        private void HandleEraseMode(EditMapTileComponent tile, Event e)
        {
            if (e.type == EventType.Repaint && tile != null)
            {
                Vector3 visualCenter = tile.transform.position + new Vector3(0.5f, 0.5f, 0.5f);
                Handles.color = new Color(1, 0, 0, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = Color.red;
                Handles.DrawWireCube(visualCenter, Vector3.one);
            }

            int controlID = GUIUtility.GetControlID("KompileMapEditor".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                GUIUtility.hotControl = controlID;
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
                if (e.type == EventType.MouseDown && e.button == 0) e.Use();
            }
            else
            {
                Handles.color = new Color(0, 1, 0, 0.3f);
                Handles.CubeHandleCap(0, visualCenter, Quaternion.identity, 0.98f, EventType.Repaint);
                Handles.color = Color.green;
                Handles.DrawWireCube(visualCenter, Vector3.one);

                if (e.type == EventType.MouseDown && e.button == 0)
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
            // 1. 수학적 마우스 위치 계산 (이전과 동일)
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

            // 2. 시각적 가이드 (Repaint)
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

            // 3. [데이터 갱신 강화] 실제 높이 변경 및 마스크 동기화
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                GUIUtility.hotControl = controlID;

                // Undo 기록 (HeightMask 필드까지 포함하기 위해 오브젝트 전체 기록)
                Undo.RecordObject(tile, "Adjust Tile Height & Mask");

                int delta = e.shift ? -1 : 1;
                bool isAnyChanged = false;

                foreach (int idx in affectedIndices)
                {
                    float currentH = tile.GetPointLocalPos(idx).y;
                    float nextH = currentH + (delta * 0.125f);

                    if (nextH >= -0.01f && nextH <= 1.01f)
                    {
                        // [데이터 수정]
                        tile.ModifyHeightIndex(idx, delta);
                        isAnyChanged = true;
                    }
                }

                if (isAnyChanged)
                {
                    // [핵심 추가] 내비게이션용 HeightMask 데이터를 실제 필드에 구워넣는 함수 호출
                    // 예: tile.RefreshNavigationData(); 또는 tile.UpdateHeightMask();
                    // 이 안에서 내부 heightData를 기반으로 필드(HeightMask)를 갱신해야 합니다.
                    tile.UpdateHeightMask();

                    // 메쉬 시각적 업데이트
                    tile.UpdateMesh();

                    // 인스펙터에 변경사항을 알리고 씬을 Dirty 상태로 만듭니다.
                    EditorUtility.SetDirty(tile);
                }

                Selection.activeGameObject = null;
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