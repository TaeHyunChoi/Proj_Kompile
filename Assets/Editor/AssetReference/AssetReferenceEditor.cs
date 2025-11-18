#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public static class AssetReferenceEditor
{
    private static readonly string[] TargetEnumIDs = new string[]
    {
        "PrefabID"
    };
    private const string ScriptOutputPath = "Assets/Script/Data/AssetID/";

    private static readonly Dictionary<string, List<EntryToProcess>> pendingMappings = new Dictionary<string, List<EntryToProcess>>();
    private static readonly StringBuilder stringBuilder = new StringBuilder();


    [MenuItem("CustomTools/Generate All Asset Maps (Generate Code & Call Mapping)")]
    public static void GenerateAllMap()
    {
        pendingMappings.Clear();
        List<EntryToProcess> entries;


        string TargetEnumID;
        string TargetAssetDirectory;

        for (int i = 0; i < TargetEnumIDs.Length; ++i)
        {
            TargetEnumID = TargetEnumIDs[i];

            // 1. 타겟 디렉토리 스캔하여 EntryToProcess 리스트 생성
            TargetAssetDirectory = GetTargetAssetDirectory(TargetEnumID);
            entries = GetEntriesFromAssets(TargetAssetDirectory);

            if (true == entries.Any()) // 왜 entries.length가 아니라 .Any()를 사용했나?
            {

                // 2-1. 스크립트 파일 생성 (enum, AssetMap.cs)
                string TargetTypeName = GetTargetTypeName(TargetEnumID);
                GenerateEnumFile(TargetEnumID, entries);
                GenerateAssetMapFile(TargetEnumID, TargetTypeName);

                // 2-2. 매핑에 필요한 데이터 저장 (이후 delayCall에서 일괄 처리)
                pendingMappings.Add(TargetEnumID, entries);
            }
            else
            {
                Debug.LogWarning($"Can`t find '{TargetEnumID}' ({TargetAssetDirectory})");
            }
        }

        if (true == pendingMappings.Any())
        {
            // 3. (모든 파일을 생성한 후) 컴파일 강제하고 .delayCall 예약
            AssetDatabase.Refresh();
            Debug.Log($"모든 파일 생성 완료. 컴파일 및 매핑 시작을 위해 delayCall 예약.");
            EditorApplication.delayCall += OnScriptsComplied;
        }
        else
        {
            Debug.LogWarning("There is No Asset Map;");
        }
    }


    private static string GetTargetTypeName(string enumID)
    {
        return enumID.Replace("ID", "");
    }
    private static string GetTargetAssetDirectory(string enumID)
    {
        string typeName = GetTargetTypeName(enumID);
        return $"Assets/Rcs/{typeName}";
    }
    private static List<EntryToProcess> GetEntriesFromAssets(string directoryPath)
    {
        List<EntryToProcess> entries = new List<EntryToProcess>();

        // 유효산 에셋 파일만 필터링
        string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(path => false == path.EndsWith(".meta"))
                        .ToArray();

        // 파일명 추출하여 entry 데이터 생성
        foreach (string filePath in files)
        {
            string unityPath = filePath.Replace('\\', '/');
            string fileName = Path.GetFileNameWithoutExtension(unityPath);
            string enumName = fileName.ToUpper();

            entries.Add(new EntryToProcess()
            {
                EnumName = enumName,
                UnityAssetPath = unityPath
            });
        }

        return entries;
    }
    private static void GenerateEnumFile(string enumID, List<EntryToProcess> entries)
    {
        stringBuilder.Clear();

        stringBuilder.AppendLine("// 이 파일은 EnumGenerator.cs에 의해 자동 생성되었습니다. 수동으로 편집하지 마세요.\n");
        stringBuilder.AppendLine($"public enum {enumID}");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine("\tNONE = 0,");

        foreach (EntryToProcess entry in entries.OrderBy(e => e.EnumName))
        {
            stringBuilder.AppendLine($"\n  {entry.EnumName},");
        }

        stringBuilder.AppendLine("}");

        string fullPath = ScriptOutputPath + enumID + ".cs";
        Directory.CreateDirectory(ScriptOutputPath);

        string script = stringBuilder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        File.WriteAllText(fullPath, script);

        Debug.Log($"Enum 파일 생성 완료: {fullPath}");
    }
    private static void GenerateAssetMapFile(string enumID, string typeName)
    {
        string mapClassName = typeName + "AssetMap";

        stringBuilder.Clear();

        stringBuilder.AppendLine("// 이 파일은 EnumGenerator.cs에 의해 자동 생성되었습니다. 수동으로 편집하지 마세요.");
        stringBuilder.AppendLine($"// 이 클래스는 {enumID} 타입의 AssetMap 데이터를 저장하는 ScriptableObject 개체입니다.\n");
        stringBuilder.AppendLine("using UnityEngine;");
        stringBuilder.AppendLine($"public class {mapClassName} : AssetMapBase<{enumID}>");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine("\t");
        stringBuilder.AppendLine("}");

        string fullPath = ScriptOutputPath + mapClassName + ".cs";
        Directory.CreateDirectory(ScriptOutputPath);

        string script = stringBuilder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        File.WriteAllText(fullPath, script);

        Debug.Log($"AssetMap 클래스 파일 생성 완료 : {fullPath}");
    }
    private static void OnScriptsComplied()
    {
        bool allTypesLoaded = false;
        int tryCount = 0;

        List<string> enumIDs = pendingMappings.Keys.ToList();
        string mapClassName;
        Type mapType;
        foreach (string enumID in enumIDs)
        {
            mapClassName = enumID.Replace("ID", "") + "AssetMap";

            mapType = GetAssetType(mapClassName);
            if (null == mapType)
            {
                break;
            }
        }

        if (true == allTypesLoaded)
        {
            Debug.Log("모든 AssetMap 클래스가 성공적으로 컴파일되었습니다. 매핑을 시작합니다.");

            foreach (var pair in pendingMappings)
            {
                AssetMapGenerator.GenerateMap(pair.Key, pair.Value);
            }

            pendingMappings.Clear();
            Debug.Log("--- Assem Map 생성 완료 ---");
        }
        else if(++tryCount <= 10) // n회 시도 해보고 안되면 끝내야겠네
        {
            EditorApplication.delayCall += OnScriptsComplied; // 재귀함수
        }
    }


    public static Type GetAssetType(string name)
    {
        Type enumType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asset => asset.GetTypes())
            .FirstOrDefault(type => type.Name == name);

        return enumType;
    }
}

#endif