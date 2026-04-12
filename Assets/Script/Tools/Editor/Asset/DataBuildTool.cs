#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using MessagePack;
using Unity.Collections;
using Script.Asset.Data;
using Script.Unit.Data;
using Script.Global.Utility;

public class DataBuildTool : EditorWindow
{
    // ====================================================================
    // [1. 설정 및 테이블 정의]
    // ====================================================================

    private const string CsvDirectory = "Assets/Editor/DataSources";
    private const string SaveDirectory = "Assets/DataBinary";

    // 구글 시트 보안 설정 (Apps Script)
    private const string WebAppUrl = "https://script.google.com/macros/s/AKfycbxGpber8YHl_X76nm-hIjaud2kpm40-ncWfBy5C9zIRLJ6YNoggPMpCRBoWDERnQbT04w/exec";
    private const string SecretToken = "KOMPILE_PRIVATE_5013!"; 

    // 테이블 관리 구조체
    private struct TableInfo
    {
        public string Name;
        public string Gid;
        public TableInfo(string name, string gid) { Name = name; Gid = gid; }
    }

    // 💡 동기화할 테이블 리스트 (새 테이블 추가 시 여기에만 등록하면 GUI에 자동 반영됩니다)
    private static readonly List<TableInfo> TargetTables = new List<TableInfo>
    {
        new TableInfo("UnitTable", "0"),
        // new TableInfo("ItemTable", "12345678"),
        // new TableInfo("SkillTable", "98765432"),
    };

    private int _selectedTableIndex = 0;
    private bool _isProcessing = false;

    // ====================================================================
    // [2. Editor Window UI 구성]
    // ====================================================================

    [MenuItem("Tools/Data/Open Data Build Window")]
    public static void ShowWindow()
    {
        GetWindow<DataBuildTool>("Data Build Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Data Table Synchronizer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 테이블 선택 섹션
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Select Table to Sync", EditorStyles.miniBoldLabel);
        
        string[] tableNames = TargetTables.ConvertAll(t => t.Name).ToArray();
        _selectedTableIndex = EditorGUILayout.Popup("Target Table", _selectedTableIndex, tableNames);
        
        EditorGUILayout.Space();

        // 버튼 섹션
        EditorGUI.BeginDisabledGroup(_isProcessing);
        
        if (GUILayout.Button($"Sync & Build [{TargetTables[_selectedTableIndex].Name}]", GUILayout.Height(30)))
        {
            SyncSelectedTableTask();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Sync & Build ALL Tables", GUILayout.Height(20)))
        {
            SyncAllTablesTask();
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();

        if (_isProcessing)
        {
            EditorGUILayout.HelpBox("Processing... Please wait.", MessageType.Info);
        }
    }

    // ====================================================================
    // [3. 실행 로직 (Task Wrapper)]
    // ====================================================================

    private async void SyncSelectedTableTask()
    {
        _isProcessing = true;
        var target = TargetTables[_selectedTableIndex];
        
        bool success = await DownloadAndBuildTable(target.Name, target.Gid);
        
        if (success) AssetDatabase.Refresh();
        _isProcessing = false;
        
        Debug.Log($"[DataBuildTool] {target.Name} 동기화 완료");
    }

    private async void SyncAllTablesTask()
    {
        _isProcessing = true;
        int successCount = 0;

        foreach (var target in TargetTables)
        {
            if (await DownloadAndBuildTable(target.Name, target.Gid)) successCount++;
        }

        AssetDatabase.Refresh();
        _isProcessing = false;
        
        Debug.Log($"[DataBuildTool] 일괄 동기화 완료 ({successCount}/{TargetTables.Count})");
    }

    // ====================================================================
    // [4. 핵심 다운로드 및 파싱 로직 (기존과 동일)]
    // ====================================================================

    private static async Task<bool> DownloadAndBuildTable(string tableName, string gid)
    {
        string url = $"{WebAppUrl}?token={SecretToken}&gid={gid}";
        string csvText = await FetchCsvFromWebAsync(url);
        
        if (string.IsNullOrEmpty(csvText) || csvText.StartsWith("Unauthorized") || csvText.StartsWith("Error"))
        {
            Debug.LogError($"[DataBuildTool] {tableName} 다운로드 실패! 사유: {csvText}");
            return false;
        }

        string csvSavePath = Path.Combine(CsvDirectory, $"{tableName}.csv").Replace("\\", "/");
        string binSavePath = Path.Combine(SaveDirectory, $"{tableName}.bin").Replace("\\", "/");

        Directory.CreateDirectory(CsvDirectory);
        Directory.CreateDirectory(SaveDirectory);
        File.WriteAllText(csvSavePath, csvText);

        return ProcessTable(tableName, csvSavePath, binSavePath);
    }

    private static async Task<string> FetchCsvFromWebAsync(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            var operation = req.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            return req.result != UnityWebRequest.Result.Success ? null : req.downloadHandler.text;
        }
    }

    private static bool ProcessTable(string tableName, string csvPath, string savePath)
    {
        try
        {
            switch (tableName)
            {
                case "UnitTable": return ParseAndSaveUnitTable(csvPath, savePath);
                default: return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataBuildTool] {tableName} 빌드 에러: {ex.Message}");
            return false;
        }
    }

#region Specific Table Parsers

    // 기존의 UnitTable 파서도 CsvParserUtil을 사용하도록 업그레이드!
    private static bool ParseAndSaveUnitTable(string csvPath, string savePath)
    {
        // 변경점: ReadAllLines 대신 ReadAllText 사용 후 CsvParserUtil 호출
        string csvText = File.ReadAllText(csvPath);
        List<string[]> rows = CsvParserUtil.Parse(csvText);
        
        var sheetList = new List<UnitTableData>();

        // i = 1 (헤더 건너뛰기)
        for (int i = 1; i < rows.Count; i++)
        {
            string[] values = rows[i];
            if (values.Length < 4 || string.IsNullOrWhiteSpace(values[0])) continue;

            sheetList.Add(new UnitTableData {
                ID = int.Parse(values[0]),
                AssetAddress = new FixedString32Bytes(values[1]),
                Type = (UnitType)Enum.Parse(typeof(UnitType), values[2]),
                BrainType = (UnitBrainType)Enum.Parse(typeof(UnitBrainType), values[3])
            });
        }

        // 정렬 및 직렬화 (기존과 동일)
        sheetList.Sort((a, b) => a.ID.CompareTo(b.ID));
        var finalSheets = sheetList.ToArray();

        var resolver = MessagePack.Resolvers.CompositeResolver.Create(
            new MessagePack.Formatters.IMessagePackFormatter[] { new FixedString32BytesFormatter() },
            new MessagePack.IFormatterResolver[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

        File.WriteAllBytes(savePath, MessagePackSerializer.Serialize(finalSheets, options));
        return true;
    }

    // 💡 새로운 로컬라이제이션(대사) 파서 추가
    private static bool ParseAndSaveLocalizationTable(string csvPath, string savePath)
    {
        string csvText = File.ReadAllText(csvPath);
        List<string[]> rows = CsvParserUtil.Parse(csvText);
        
        var sheetList = new List<LocalizationTableData>();

        for (int i = 1; i < rows.Count; i++)
        {
            string[] values = rows[i];
            if (values.Length < 4 || string.IsNullOrWhiteSpace(values[0])) continue;

            sheetList.Add(new LocalizationTableData {
                ID = int.Parse(values[0]),
                Key = new FixedString32Bytes(values[1]),
                KR = values[2],  // 쉼표나 줄바꿈이 있어도 안전하게 통째로 들어옵니다!
                EN = values[3]
            });
        }

        sheetList.Sort((a, b) => a.ID.CompareTo(b.ID));
        var finalSheets = sheetList.ToArray();

        var resolver = MessagePack.Resolvers.CompositeResolver.Create(
            new MessagePack.Formatters.IMessagePackFormatter[] { new FixedString32BytesFormatter() },
            new MessagePack.IFormatterResolver[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

        File.WriteAllBytes(savePath, MessagePackSerializer.Serialize(finalSheets, options));
        Debug.Log($"[DataBuildTool] Localization 빌드 완료 ({savePath})");
        return true;
    }

    #endregion
}
#endif