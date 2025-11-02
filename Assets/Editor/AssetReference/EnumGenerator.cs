using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using JetBrains.Annotations;


/// <summary> for test:
/// string builder로 코드 텍스트를 만들어서 파일에 저장하는거구나?
/// </summary>
public class EnumGenerator
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
        string[] enumNames;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// --------------------------------------------------");
        sb.AppendLine($"// 이 파일은 에디터 스크립트(EnumGenerator.cs)에 의하여 자동으로 생성됩니다.");
        sb.AppendLine($"// 수동으로 편집하지 마십시오.");
        sb.AppendLine("// --------------------------------------------------");

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

            // 파일 이름에서 Enum 값으로 이름 추출 및 정리
            enumNames = files.Select(Path.GetFileNameWithoutExtension) // 확장자 제외한 파일 이름만
                            .Where(name => false == string.IsNullOrEmpty(name)) // 유효값만
                            .Distinct() // 중복 제외
                            .ToArray();

            if (0 == enumNames.Length)
            {
                Debug.LogError($"경로 {path}에서 처리할 파일을 찾지 못했습니다.");
                return;
            }

            sb.AppendLine($"public enum ASSET_{ASSET_DIRECTORIES[i].Replace("Rcs/", "")}");
            sb.AppendLine("{");
            sb.AppendLine("\tNone = 0,");

            int index = 1;
            string valueName;
            foreach (string name in enumNames.OrderBy(n => n)) // 정렬하여 순서를 일정하게 하는 것이 좋다고 하는데..
            {
                // 파일명에 포함될 수 있는 특수 문자를 언더 스코어(_)로 치환하는 등의 추가 로직 필요할지도?
                valueName = name.Replace("-","_");
                sb.AppendLine($"\t{valueName} = {index++},");
            }
            sb.AppendLine("}");

            total += index;
        }

        // 파일 저장, UnityEditor 갱신

        // UnityEditor 사용하니까 상대 경로를 사용 => "Assets/" 삭제
        string finalPath = Path.Combine(assetsPath, ENUM_FILE_PATH.Replace("Assets/", ""));
        File.WriteAllText(finalPath, sb.ToString());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"ItemID Enum 생성 완료 (파일명 기반). 총 {ASSET_DIRECTORIES.Length}개 경로에서 {total}개 항목.");
    }
}
