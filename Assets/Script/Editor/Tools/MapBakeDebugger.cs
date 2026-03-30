#if UNITY_EDITOR
namespace Script.Map.Editor
{
    using Script.Map.Data;
    using Script.Asset.Provider;
    using UnityEditor;
    using UnityEngine;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// [Framework] Utility: Bake()된 바이너리 맵 데이터를 읽어와 구조와 비트마스크를 분석하는 전용 뷰어 창입니다.
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
                MapGridData gridData = await AssetRepoProvider.LoadBinaryDataAsync<MapGridData>(fileName);

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
            EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
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
            // 우측 패널: 선택된 Grid의 상세 데이터 (Navi, Link 마스크 확인)
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

            // 1. 조립해야 할 메쉬 에셋 정보
            EditorGUILayout.LabelField("📦 Required Mesh Assets", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (grid.layerMeshAssets != null && grid.layerMeshAssets.Count > 0)
            {
                // [수정 완료] Dictionary 순회에서 List<MapGridLayerData> 객체 순회로 일치
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

            // 2. 타일 논리 데이터 (NaviMask, LinkMask)
            EditorGUILayout.LabelField($"🟩 Tile Logical Data (Total: {grid.NaviTileDict?.Count ?? 0})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("NaviMask: 상위 비트(Layer) + 하위 비트(13정점 높이)\nLinkMask: 8방향 이웃 타일과의 이동 가능 여부 비트", MessageType.Info);
            
            if (grid.NaviTileDict == null || grid.NaviTileDict.Count == 0) return;

            // 표 헤더
            EditorGUILayout.BeginHorizontal("toolbar");
            GUILayout.Label("Tile Key", GUILayout.Width(80));
            GUILayout.Label("Layer", GUILayout.Width(50));
            GUILayout.Label("Navi Mask (64-bit)", GUILayout.Width(500));
            GUILayout.Label("Link Mask (16-bit)", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            _tileScrollPos = EditorGUILayout.BeginScrollView(_tileScrollPos);

            // [수정 완료] EditMapTileData가 아닌 MessagePack용 런타임 MapTileData로 순회 처리
            foreach (var kvp in grid.NaviTileDict)
            {
                int tileKey = kvp.Key;
                MapTileData tileData = kvp.Value;

                EditorGUILayout.BeginHorizontal();
                
                // 타일 키 & 레이어 마스크 (RenderIndex 프로퍼티 제거에 따른 대응)
                GUILayout.Label(tileKey.ToString(), GUILayout.Width(80));
                GUILayout.Label(tileData.LayerMask.ToString(), GUILayout.Width(50));

                // NaviMask (64비트를 보기 좋게 8자리씩 끊어서 출력)
                string naviBin = FormatBinaryString((long)tileData.NaviMask, 64);
                GUILayout.Label(naviBin, EditorStyles.wordWrappedMiniLabel, GUILayout.Width(500));

                // LinkMask는 구조체 정의에 따라 ushort(16비트)로 출력합니다.
                string linkBin = FormatBinaryString((long)tileData.LinkMask, 16);
                GUILayout.Label(linkBin, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 정수 값을 이진수 문자열로 변환하고, 가독성을 위해 8비트 단위로 띄어쓰기를 삽입합니다.
        /// </summary>
        private string FormatBinaryString(long value, int totalBits)
        {
            string bin = Convert.ToString(value, 2).PadLeft(totalBits, '0');
            var sb = new System.Text.StringBuilder();
            
            for (int i = 0; i < bin.Length; i++)
            {
                if (i > 0 && (bin.Length - i) % 8 == 0)
                {
                    sb.Append(" "); // 8비트마다 공백
                }
                sb.Append(bin[i]);
            }
            return sb.ToString();
        }
    }
}
#endif