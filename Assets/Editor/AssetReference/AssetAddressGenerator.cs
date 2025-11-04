using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary> for test:
/// string builder로 코드 텍스트를 만들어서 파일에 저장하는거구나?
/// </summary>
public class AssetAddressGenerator
{
    // 폴더 별로 분류하는게 일단은 나아보인다. 우선 이정도?
    // Editor 안에서 찾는 것이므로 상대 경로 - "Assets\" 를 붙이지 않는다.
    private static readonly string[] ASSET_DIRECTORIES = 
        {
            "Rcs/Prefab",
            //"Rcs/Mesh",
            //"Rcs/Sprite"
        };

    // 로컬 컴퓨터에서 경로를 직접 찾아 입력한다 =>  "Assets\" 붙인다. 앞에는 Unity 로컬 경로가 붙는다.
    private const string ENUM_FILE_PATH = "Assets/Script/Data/AddressableAssetKeys.cs";


    [MenuItem("Tools/Asset Management/Generate Enums From Folders")]
    public static void GenerateEnums()
    {
        string assetsPath = Application.dataPath;
        string path;
        string[] files;
        string enumName;
        List<EntryToProcess> entries = new List<EntryToProcess>();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// --------------------------------------------------");
        sb.AppendLine($"// 이 파일은 에디터 스크립트(EnumGenerator.cs)에 의하여 자동으로 생성됩니다.");
        sb.AppendLine($"// 수동으로 편집하지 마십시오.");
        sb.AppendLine("// --------------------------------------------------\n");

        int total = 0;
        for (int i = 0; i < ASSET_DIRECTORIES.Length; ++i)
        {
            path = Path.Combine(Application.dataPath, ASSET_DIRECTORIES[i]);
            path = path.Replace('/', Path.DirectorySeparatorChar);

            if (false == Directory.Exists(path))
            {
                Debug.LogError($"대상 디렉토리를 찾을 수 없습니다: {path}");
                return;
            }

            // 대상 디렉토리 내의 모든 파일 목록 가져오기
            files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                             .Where(file => false == file.EndsWith(".meta"))
                             .ToArray();


            for (int f = 0; f < files.Length; ++f)
            {
                enumName = Path.GetFileNameWithoutExtension(files[f]);

                // 유효한 이름만
                if (true == string.IsNullOrEmpty(enumName))
                {
                    continue;
                }

                entries.Add(new EntryToProcess()
                {
                    EnumName        = enumName,
                    UnityAssetPath  = files[f]
                });
            }

            if (0 == entries.Count)
            {
                Debug.LogError($"경로 {path}에서 처리할 파일을 찾지 못했습니다.");
                return;
            }

            entries = entries.Distinct().ToList();                  // 중복 제외
            entries = entries.OrderBy(e => e.EnumName).ToList();    // 오름차순 정렬

            enumName = $"ASSET_{ASSET_DIRECTORIES[i].Replace("Rcs/", "")}";
            sb.AppendLine($"public enum {enumName}");
            sb.AppendLine("{");
            sb.AppendLine("\tNone = 0,");

            int index = 1;
            string valueName;

            for (int v = 0; v < entries.Count; ++v)
            {
                valueName = entries[v].EnumName;
                sb.AppendLine($"\t{valueName} = {index++},");
            }

            sb.AppendLine("}");
            total += index;
        }

        // 파일 저장, UnityEditor 갱신

        // UnityEditor 사용하니까 상대 경로를 사용 => "Assets/" 삭제
        string finalPath = Path.Combine(assetsPath, ENUM_FILE_PATH.Replace("Assets/", ""));
        string content = sb.ToString().Replace("\r\n", "\n");
        File.WriteAllText(finalPath, content);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"ItemID Enum 생성 완료 (파일명 기반). 총 {ASSET_DIRECTORIES.Length}개 경로에서 {total}개 항목.");

        // ScriptableObject에 저장한단 말이지?...
        EditorApplication.delayCall += () => AssetMapGenerator.GenerateMap(entries);
    }
}
