#if UNITY_EDITOR
namespace  Kompile.Asset.Editor.Tools
{
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.Networking;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using MessagePack;
    using MessagePack.Formatters;
    using MessagePack.Resolvers;
    using Unity.Collections;
    using Kompile.Asset.Data;
    using Kompile.Asset.Utility;
    using Kompile.Unit.Data;

    public class DataBuildTool : EditorWindow
    {
        private const string CsvDirectory = "Assets/Rcs/Bytes/Table/Editor";
        private const string SaveDirectory = "Assets/Rcs/Bytes/Table";
        private const string AddressableGroupName = "DataTable";

        // 구글 시트 보안 설정
        // 시트 링크: https://docs.google.com/spreadsheets/d/1H3Gn8GfkjLo4e5_7DCUf0GM-EtAmPFG-s_SaP2nDZmE/edit?gid=0#gid=0
        private const string WebAppUrl =
            "https://script.google.com/macros/s/AKfycbxGpber8YHl_X76nm-hIjaud2kpm40-ncWfBy5C9zIRLJ6YNoggPMpCRBoWDERnQbT04w/exec";

        private const string SecretToken = "KOMPILE_PRIVATE_5013!";

        private struct TableInfo
        {
            public string Name;
            public string Gid;

            public TableInfo(string name, string gid)
            {
                Name = name;
                Gid = gid;
            }
        }

        private static readonly List<TableInfo> TargetTables = new List<TableInfo>
        {
            //new TableInfo("UnitTable", "0"),
            new TableInfo("FieldUnitTable", "86365781"),
        };

        private int _selectedTableIndex = 0;
        private bool _isProcessing = false;

        [MenuItem("Tools/Data/Open Data Build Window")]
        public static void ShowWindow() => GetWindow<DataBuildTool>("Data Build Tool");

        private void OnGUI()
        {
            GUILayout.Label("Data Table Synchronizer (Unity 6)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            string[] tableNames = TargetTables.ConvertAll(t => t.Name).ToArray();
            _selectedTableIndex = EditorGUILayout.Popup("Target Table", _selectedTableIndex, tableNames);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(_isProcessing);
            if (GUILayout.Button($"Sync & Build & Address [{TargetTables[_selectedTableIndex].Name}]",
                    GUILayout.Height(30)))
                SyncSelectedTableTask();

            if (GUILayout.Button("Sync & Build ALL & Address", GUILayout.Height(20)))
                SyncAllTablesTask();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }

        private async void SyncSelectedTableTask()
        {
            _isProcessing = true;
            var target = TargetTables[_selectedTableIndex];
            if (await DownloadAndBuildTable(target.Name, target.Gid))
            {
                AssetDatabase.Refresh();
                RegisterToAddressable(target.Name);
            }

            _isProcessing = false;
            Repaint();
        }

        private async void SyncAllTablesTask()
        {
            _isProcessing = true;
            foreach (var target in TargetTables)
            {
                if (await DownloadAndBuildTable(target.Name, target.Gid))
                {
                    AssetDatabase.Refresh();
                    RegisterToAddressable(target.Name);
                }
            }

            _isProcessing = false;
            Repaint();
        }

        private static async Task<bool> DownloadAndBuildTable(string tableName, string gid)
        {
            string url = $"{WebAppUrl}?token={SecretToken}&gid={gid}";
            string csvText = await FetchCsvFromWebAsync(url);

            if (string.IsNullOrEmpty(csvText) || csvText.StartsWith("Error")) return false;

            Directory.CreateDirectory(CsvDirectory);
            Directory.CreateDirectory(SaveDirectory);

            string csvPath = Path.Combine(CsvDirectory, $"{tableName}.csv");

            // 💡 수정됨: 확장자를 .bin에서 .bytes로 변경하여 Unity가 TextAsset으로 인식하게 함
            string binPath = Path.Combine(SaveDirectory, $"{tableName}.bytes");

            File.WriteAllText(csvPath, csvText);
            return ProcessTable(tableName, csvPath, binPath);
        }

        private static async Task<string> FetchCsvFromWebAsync(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.redirectLimit = 5;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Delay(10);
            return req.result != UnityWebRequest.Result.Success ? null : req.downloadHandler.text;
        }

        private static bool ProcessTable(string tableName, string csvPath, string savePath)
        {
            try
            {
                return tableName switch
                {
                    "UnitTable" => ParseAndSaveUnitTable(csvPath, savePath),
                    "FieldUnitTable" => ParseAndSaveFieldUnitTable(csvPath, savePath),
                    _ => false
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataBuildTool] {tableName} 빌드 에러: {e.Message}");
                return false;
            }
        }

        private static void RegisterToAddressable(string tableName)
        {
            // 💡 수정됨: 어드레서블 등록 시에도 경로를 .bytes로 찾도록 수정
            string assetPath = Path.Combine(SaveDirectory, $"{tableName}.bytes").Replace("\\", "/");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            // 1. 그룹 찾기 또는 생성
            var group = settings.FindGroup(AddressableGroupName);
            if (group == null) group = settings.CreateGroup(AddressableGroupName, false, false, true, null);

            // 2. 에셋 엔트리 생성/업데이트
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);

            // 3. 주소 설정 (파일명으로 설정)
            entry.address = tableName;

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            Debug.Log($"[DataBuildTool] {tableName} 어드레서블 등록 완료 (Group: {AddressableGroupName})");
        }

        private static bool ParseAndSaveUnitTable(string csvPath, string savePath)
        {
            string csvText = File.ReadAllText(csvPath);
            var rows = CsvParserUtil.Parse(csvText);
            var sheetList = new List<UnitTableData>();

            for (int i = 1; i < rows.Count; i++)
            {
                var v = rows[i];
                if (v.Length < 4 || string.IsNullOrWhiteSpace(v[0])) continue;
                sheetList.Add(new UnitTableData
                {
                    ID = int.Parse(v[0]),
                    AssetAddress = new FixedString32Bytes(v[1]),
                    Type = (UnitType)Enum.Parse(typeof(UnitType), v[2]),
                    BrainType = (UnitBrainType)Enum.Parse(typeof(UnitBrainType), v[3]),
                    AocAddress = new FixedString32Bytes(v[4])
                });
            }

            sheetList.Sort((a, b) => a.ID.CompareTo(b.ID));
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { new FixedString32BytesFormatter() },
                    new IFormatterResolver[] { ContractlessStandardResolver.Instance }
                )
            );
            File.WriteAllBytes(savePath, MessagePackSerializer.Serialize(sheetList.ToArray(), options));
            return true;
        }
        private static bool ParseAndSaveFieldUnitTable(string csvPath, string savePath)
        {
            string csvText = File.ReadAllText(csvPath);
            var rows = CsvParserUtil.Parse(csvText);
            var sheetList = new List<FieldUnitTableData>();

            for (int i = 1; i < rows.Count; i++)
            {
                var v = rows[i];
                if (v.Length < 4 || string.IsNullOrWhiteSpace(v[0]))
                {
                    continue;
                }
                
                sheetList.Add(new FieldUnitTableData
                {
                    Index = int.Parse(v[0]),
                    NameKey = new FixedString32Bytes(v[1]),
                    BrainType = (UnitBrainType)Enum.Parse(typeof(UnitBrainType), v[2]),
                    CollisionRange = float.Parse(v[3])
                });
            }

            sheetList.Sort((a, b) => a.Index.CompareTo(b.Index));
            var options = MessagePackSerializerOptions.Standard.WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { new FixedString32BytesFormatter() },
                    new IFormatterResolver[] { ContractlessStandardResolver.Instance }
                )
            );
            File.WriteAllBytes(savePath, MessagePackSerializer.Serialize(sheetList.ToArray(), options));
            return true;
        }
    }
}
#endif