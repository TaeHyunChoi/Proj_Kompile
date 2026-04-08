#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Script.Map.Data;
    using Script.Map.Utility; // MapCoordUtil 사용을 위해 추가
    using Script.Global.Asset.Provider;
    using UnityEditor;
    using UnityEngine;
    using Unity.Mathematics;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// [Framework] Utility: Bake()된 바이너리 맵 데이터를 읽어와 구조와 비트마스크를 직관적으로 분석하는 전용 뷰어 창입니다.
    /// </summary>
    public class MapBakeDebugger : EditorWindow
    {
        private Dictionary<int, MapGridData> _loadedGrids = new Dictionary<int, MapGridData>();
        private int _selectedGridKey = -1;

        private Vector2 _gridScrollPos;
        private Vector2 _tileScrollPos;

        [MenuItem("Tools/Map/Debug Baked Map Data")]
        public static void ShowWindow()
        {
            GetWindow<MapBakeDebugger>("Map Bake Debugger").Show();
        }

        private void OnEnable()
        {
            LoadAllBakedData();
        }

        private async void LoadAllBakedData()
        {
            _loadedGrids.Clear();
            string path = "Assets/Rcs/Bytes/MapNavi";

            if (!Directory.Exists(path)) return;

            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
            {
                if (file.EndsWith(".meta")) continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                MapGridData gridData = await AssetProvider.ReadBinaryDataAsync<MapGridData>(fileName);

                if (gridData != null)
                {
                    _loadedGrids[gridData.Key] = gridData;
                }
            }

            if (_loadedGrids.Count > 0)
            {
                _selectedGridKey = _loadedGrids.Keys.First();
            }
            
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🗺️ Map Binary Data Viewer", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 데이터 리로드", GUILayout.Width(120), GUILayout.Height(25)))
            {
                LoadAllBakedData();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_loadedGrids.Count == 0)
            {
                EditorGUILayout.HelpBox("구워진 맵 데이터가 없습니다. Bake()를 먼저 실행해주세요.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // =========================================================================
            // 좌측 패널: 구역(Grid) 리스트
            // =========================================================================
            EditorGUILayout.BeginVertical("box", GUILayout.Width(150));
            GUILayout.Label("Grid List", EditorStyles.boldLabel);
            _gridScrollPos = EditorGUILayout.BeginScrollView(_gridScrollPos);

            foreach (var gridKey in _loadedGrids.Keys)
            {
                GUI.backgroundColor = (_selectedGridKey == gridKey) ? Color.cyan : Color.white;
                if (GUILayout.Button($"Grid [{gridKey}]", GUILayout.Height(30)))
                {
                    _selectedGridKey = gridKey;
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // =========================================================================
            // 우측 패널: 선택된 Grid의 상세 데이터
            // =========================================================================
            EditorGUILayout.BeginVertical("box");
            if (_loadedGrids.TryGetValue(_selectedGridKey, out MapGridData selectedGrid))
            {
                DrawGridDetails(selectedGrid);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGridDetails(MapGridData grid)
        {
            GUILayout.Label($"Grid [{grid.Key}] Details", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("📦 Required Mesh Assets", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (grid.layerMeshAssets != null && grid.layerMeshAssets.Count > 0)
            {
                foreach (MapGridLayerData layerData in grid.layerMeshAssets)
                {
                    string assets = (layerData.assets != null) ? string.Join(", ", layerData.assets) : "None";
                    EditorGUILayout.LabelField($"Layer {layerData.layer}: {assets}");
                }
            }
            else
            {
                EditorGUILayout.LabelField("None");
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"🟩 Tile Logical Data (Total: {grid.NaviTileDict?.Count ?? 0})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Pivot: Grid/Tile Key를 변환한 월드 좌표\nNavi: 13개 정점의 높이 배열 (-1 ~ 8)\nLink: 8방향 이웃 타일과의 Y 단차 (0, 1, -1)", MessageType.Info);
            
            if (grid.NaviTileDict == null || grid.NaviTileDict.Count == 0) return;

            // 표 헤더
            EditorGUILayout.BeginHorizontal("toolbar");
            GUILayout.Label("Tile Key", GUILayout.Width(70));
            GUILayout.Label("Pivot (X, Y, Z)", GUILayout.Width(120));
            GUILayout.Label("Navi Mask (Heights)", GUILayout.Width(250));
            GUILayout.Label("Link Mask (Diff Y)", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            _tileScrollPos = EditorGUILayout.BeginScrollView(_tileScrollPos);

            foreach (var kvp in grid.NaviTileDict)
            {
                int tileKey = kvp.Key;
                MapTileData tileData = kvp.Value;

                EditorGUILayout.BeginHorizontal();
                
                // 1. 타일 키
                GUILayout.Label(tileKey.ToString(), GUILayout.Width(70));

                // 2. Pivot (X, Y, Z) 추출
                // 💡 프로젝트 내 MapCoordUtil의 좌표 추출 함수 이름에 맞게 수정해주세요. (예: GetPosition, GetPivot 등)
                string pivotStr = "Unknown";
                try
                {
                    // [Framework] 관례상 ComputeKey의 반대 기능을 수행하는 함수를 호출합니다.
                    MapCoordUtil.GetPivot(grid.Key, tileKey, out float3 pivot); 
                    pivotStr = $"({pivot.x:F1}, {pivot.y:F1}, {pivot.z:F1})";
                }
                catch
                {
                    pivotStr = "Func Mismatch";
                }
                GUILayout.Label(pivotStr, GUILayout.Width(120));

                // 3. Navi Mask 파싱 (13개 정점의 높이값)
                string naviParsed = ParseNaviMask((long)tileData.NaviMask);
                GUILayout.Label(naviParsed, EditorStyles.wordWrappedMiniLabel, GUILayout.Width(250));

                // 4. Link Mask 파싱 (8방향의 Y 단차)
                string linkParsed = ParseLinkMask(tileData.LinkMask);
                GUILayout.Label(linkParsed, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// NaviMask의 하위 52비트를 파싱하여 13개 정점의 높이 배열로 반환합니다.
        /// (각 정점당 4비트, 15(0xF)는 -1(삭제됨)로 취급)
        /// </summary>
        private string ParseNaviMask(long naviMask)
        {
            List<int> heights = new List<int>(13);
            for (int i = 0; i < 13; i++)
            {
                // 4비트 단위로 값을 추출
                int h = (int)((naviMask >> (i * 4)) & 0xF);
                // 0xF (15)는 삭제된 버텍스인 -1로 복원
                heights.Add(h == 15 ? -1 : h); 
            }
            return $"[{string.Join(", ", heights)}]";
        }

        /// <summary>
        /// LinkMask의 16비트를 파싱하여 8방향의 단차 배열로 반환합니다.
        /// </summary>
        private string ParseLinkMask(ushort linkMask)
        {
            List<string> diffYs = new List<string>(8);
            for (int i = 0; i < 8; i++)
            {
                int val = (linkMask >> (i * 2)) & 0x3;
                
                string parsedVal = val switch
                {
                    0 => "0",   // LINK_ZERO
                    1 => "1",   // LINK_UP
                    2 => "-1",  // LINK_DOWN
                    3 => "X",   // LINK_NONE (연결 끊김)
                    _ => "?"
                };
                diffYs.Add(parsedVal);
            }
            return $"[{string.Join(", ", diffYs)}]";
        }
    }
}
#endif