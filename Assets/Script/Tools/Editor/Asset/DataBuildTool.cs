#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;
using MessagePack;
using Unity.Collections;
using Script.Global.Asset.Data;
using Script.Global.Unit.Data;

public class DataBuildTool : EditorWindow
{
    // ====================================================================
    // [1. 경로 및 API 설정]
    // ====================================================================

    // 로컬 디렉토리 경로
    private const string CsvDirectory = "Assets/Editor/Data/CSV";
    private const string SaveDirectory = "Assets/Rcs/Bytes/Table";

    // 💡 구글 시트 보안 연동 (Apps Script) 설정 💡
    // Apps Script 배포 후 얻은 웹 앱 URL을 아래에 입력하세요.
    private const string WebAppUrl = "https://script.google.com/macros/s/AKfycbxGpber8YHl_X76nm-hIjaud2kpm40-ncWfBy5C9zIRLJ6YNoggPMpCRBoWDERnQbT04w/exec";
    
    // Apps Script 코드에 작성한 SECRET_TOKEN과 동일하게 입력하세요.
    private const string SecretToken = "KOMPILE_PRIVATE_5013!"; 

    // 테이블별 GID 매핑 (구글 시트 URL 끝에 있는 gid 번호)
    private const string UnitTableGID = "0"; 


    // ====================================================================
    // [2. 메뉴 1: 구글 시트 동기화 및 전체 빌드 (자동)]
    // ====================================================================

    [MenuItem("Tools/Data/Sync & Build All Tables (Google Sheets)")]
    public static async void SyncAndBuildAllTables()
    {
        Directory.CreateDirectory(CsvDirectory);
        Directory.CreateDirectory(SaveDirectory);

        Debug.Log("[DataBuildTool] 구글 시트 동기화 시작...");

        int successCount = 0;

        // UnitTable 다운로드 및 빌드
        if (await DownloadAndBuildTable("UnitTable", UnitTableGID)) 
        {
            successCount++;
        }
        
        // 향후 테이블이 추가되면 아래에 줄을 추가하십시오.
        // if (await DownloadAndBuildTable("CharacterTable", "123456789")) successCount++;

        // 에셋 데이터베이스 단 1회 갱신
        AssetDatabase.Refresh();
        Debug.Log($"[DataBuildTool] 전체 파이프라인 완료! ({successCount}개 테이블 성공)");
    }

    private static async Task<bool> DownloadAndBuildTable(string tableName, string gid)
    {
        string url = $"{WebAppUrl}?token={SecretToken}&gid={gid}";
        
        string csvSavePath = Path.Combine(CsvDirectory, $"{tableName}.csv").Replace("\\", "/");
        string binSavePath = Path.Combine(SaveDirectory, $"{tableName}.bin").Replace("\\", "/");

        // 1. 웹에서 CSV 텍스트 비동기 다운로드
        string csvText = await FetchCsvFromWebAsync(url);
        
        // 인증 실패 또는 에러 처리
        if (string.IsNullOrEmpty(csvText) || csvText.StartsWith("Unauthorized") || csvText.StartsWith("Error"))
        {
            Debug.LogError($"[DataBuildTool] {tableName} 다운로드 실패! 사유: {csvText}");
            return false;
        }

        // 2. 로컬에 CSV 원본 저장 (버전 관리용)
        File.WriteAllText(csvSavePath, csvText);

        // 3. 파싱 및 바이너리 빌드
        return ProcessTable(tableName, csvSavePath, binSavePath);
    }

    private static async Task<string> FetchCsvFromWebAsync(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            var operation = req.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DataBuildTool] Network Error: {req.error}");
                return null;
            }

            return req.downloadHandler.text;
        }
    }


    // ====================================================================
    // [3. 메뉴 2: 로컬 CSV 전체 빌드 (오프라인 수동용)]
    // ====================================================================

    [MenuItem("Tools/Data/Build All Tables (Local CSV Only)")]
    public static void BuildAllTables()
    {
        if (!Directory.Exists(CsvDirectory))
        {
            Debug.LogError($"[DataBuildTool] Source directory not found: {CsvDirectory}");
            return;
        }

        Directory.CreateDirectory(SaveDirectory);

        string[] csvFiles = Directory.GetFiles(CsvDirectory, "*.csv");
        int successCount = 0;

        foreach (string csvPath in csvFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            string savePath = Path.Combine(SaveDirectory, $"{fileName}.bin").Replace("\\", "/");

            bool success = ProcessTable(fileName, csvPath, savePath);
            if (success) 
            {
                successCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[DataBuildTool] Local Batch Build Complete! ({successCount}/{csvFiles.Length} tables built.)");
    }


    // ====================================================================
    // [4. 핵심 파서 및 라우팅 로직]
    // ====================================================================

    /// <summary>
    /// 파일명에 따라 적합한 파서(Parser)로 라우팅하는 역할
    /// </summary>
    private static bool ProcessTable(string tableName, string csvPath, string savePath)
    {
        try
        {
            switch (tableName)
            {
                case "UnitTable":
                    return ParseAndSaveUnitTable(csvPath, savePath);
                
                // 향후 새로운 테이블이 추가되면 여기에 case를 추가하십시오.
                // case "CharacterTable":
                //     return ParseAndSaveCharacterTable(csvPath, savePath);

                default:
                    Debug.LogWarning($"[DataBuildTool] Parser for '{tableName}' is not defined. Skipping.");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataBuildTool] Failed to build '{tableName}'. Exception: {ex.Message}");
            return false;
        }
    }

    #region Specific Table Parsers
    
    private static bool ParseAndSaveUnitTable(string csvPath, string savePath)
    {
        string[] lines = File.ReadAllLines(csvPath);
        
        // 1. 임시 List 할당
        System.Collections.Generic.List<UnitTableData> sheetList = new System.Collections.Generic.List<UnitTableData>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');

            sheetList.Add(new UnitTableData
            {
                ID = int.Parse(values[0]),
                AssetAddress = new FixedString32Bytes(values[1]),
                Type = (UnitType)System.Enum.Parse(typeof(UnitType), values[2]),
                BrainType = (UnitBrainType)System.Enum.Parse(typeof(UnitBrainType), values[3])
            });
        }

        // 💡 중요: Provider의 이진 탐색(Binary Search)이 완벽히 작동하도록 ID 기준으로 오름차순 정렬
        sheetList.Sort((a, b) => a.ID.CompareTo(b.ID));

        // 2. 최종 구조체 배열(Array)로 변환
        UnitTableData[] finalSheets = sheetList.ToArray();

        // 3. MessagePack Custom Resolver 세팅
        var resolver = MessagePack.Resolvers.CompositeResolver.Create(
            new MessagePack.Formatters.IMessagePackFormatter[] { new FixedString32BytesFormatter() },
            new MessagePack.IFormatterResolver[] { MessagePack.Resolvers.ContractlessStandardResolver.Instance }
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

        // 4. 배열 자체를 직렬화하여 저장
        byte[] byteArray = MessagePackSerializer.Serialize(finalSheets, options);
        File.WriteAllBytes(savePath, byteArray);
        
        Debug.Log($"[DataBuildTool] Built: {System.IO.Path.GetFileName(savePath)} ({byteArray.Length} bytes)");
        return true;
    }

    #endregion
}
#endif