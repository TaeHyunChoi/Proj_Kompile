using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using CDataStructure;

/// <summary>
/// [목표] CSV 데이터 불러오기 최적화 (속도 향상을 위하여 안정성은 상대적으로 낮춤)
/// [방법] (1)기존 string → byte[] 로 읽어오기 (2)Generic<T> 활용하여 코드 재사용성 높이기
/// [비고] Job System을 사용하여 멀티 스레딩으로 CSV를 불러오려 했으나 'reference type은 Job struct에서 사용할 수 없다'는 제한이 있어 기각.
/// </summary>
public class DataTable
{
    private static List<SkillData> skillTable;
    private static List<ItemData>  itemTable;
    private static List<UnitData>  unitTalbe;
    private static List<MapData>   mapTable;

    public static List<SkillData> SkillTable { get => skillTable; }
    public static List<ItemData>  ItemTable  { get => itemTable; }
    public static List<UnitData>  UnitTable  { get => unitTalbe; }
    public static List<MapData>   MapTable     { get => mapTable; }


#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    //.csv (기획자, 에디터에서 .bin 파일로 변환 필요)
    public static void LoadCSVTable()
    {
        skillTable = LoadTable<SkillData>("SkillData");
        itemTable = LoadTable<ItemData>("ItemData");
        unitTalbe = LoadTable<UnitData>("UnitData");
        mapTable = LoadTable<MapData>("MapData");
    }
    private static List<T> LoadTable<T>(string fileName) where T : IDataSetter, new()
    {
        List<Dictionary<string, string>> table = new List<Dictionary<string, string>>();
        TextAsset csv = Resources.Load<TextAsset>("CSV/" + fileName);
        StringReader reader = new StringReader(csv.text);
        StringBuilder sb = new StringBuilder();

        //Setting
        string[] columns;   //칼럼명
        int index;          //칼럼명[] 인덱스
        string line;        //각 줄
        char[] chars;       //각 줄을 char 형태로 쪼갬 (중간 ,를 발라내기 위함)
        bool isSplit;       //분류 여부 (대사 등 본문의 ,와 CSV 구분쉼표를 구분하기 위함)

        //Column Index
        line = reader.ReadLine(); //첫줄 날리기
        columns = line.Split(',');

        //Content
        while (true)
        {
            line = reader.ReadLine();
            if (line == null)
                break;

            Dictionary<string, string> data = new Dictionary<string, string>();
            chars = line.ToCharArray();
            isSplit = true;
            index = -1;

            for (int i = 0; i < chars.Length; ++i)
            {
                //데이터 중간의 ,로 나누지 않기 위해 판별 조건 추가
                if (chars[i] == '\u0022') //큰따옴표(")의 유니코드
                {
                    isSplit = !isSplit;
                    continue;
                }

                if (isSplit
                    && chars[i] == '\u002C') //쉼표(,) 유니코드
                {
                    data.Add(columns[++index], sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(chars[i]);
            }

            //마지막 데이터 추가 (,가 없어서 위에서 안걸림)
            data.Add(columns[++index], sb.ToString());
            table.Add(data);
            sb.Clear();
        }

        List<T> list = new List<T>();
        for (int i = 0; i < table.Count; ++i)
        {
            T tData = new T();
            tData.Set(table[i]);
            list.Add(tData);
        }

        return list;
    }


    // map sampling
    public static void WriteBinaryMappingData(Dictionary<int, Voxel_t> data, string fileName)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapVoxelData", fileName + ".dat");
        FileStream fileStream = File.Create(filePath);

        // Dictionary 직렬화
        binaryFormatter.Serialize(fileStream, data);

        fileStream.Close();
    }
    public static Dictionary<int, Voxel_t> LoadMappingData(string fileName)
    {
        string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapVoxelData", fileName + ".dat");
        if (File.Exists(filePath))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = File.Open(filePath, FileMode.Open);

            // 파일에서 데이터를 역직렬화하여 Dictionary에 로드
            Dictionary<int, Voxel_t> map = (Dictionary<int, Voxel_t>)binaryFormatter.Deserialize(fileStream);

            fileStream.Close();
            return map;
        }
        else
        {
            Debug.LogError("파일이 존재하지 않습니다.");
        }

        return null;
    }
    //public static void WriteBinaryMapVoxel(Dictionary<int, Voxel_t> data, string fileName)
    //{
    //    BinaryFormatter binaryFormatter = new BinaryFormatter();
    //    string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapVoxelData", fileName + ".dat");
    //    FileStream fileStream = File.Create(filePath);

    //    // Dictionary 직렬화
    //    binaryFormatter.Serialize(fileStream, data);

    //    fileStream.Close();
    //}
    //public static Dictionary<int, Voxel_t> LoadMapVoxel(string fileName)
    //{
    //    string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapVoxelData", fileName + ".dat");
    //    if (File.Exists(filePath))
    //    {
    //        BinaryFormatter binaryFormatter = new BinaryFormatter();
    //        FileStream fileStream = File.Open(filePath, FileMode.Open);

    //        // 파일에서 데이터를 역직렬화하여 Dictionary에 로드
    //        Dictionary<int, Voxel_t> data = (Dictionary<int, Voxel_t>)binaryFormatter.Deserialize(fileStream);

    //        fileStream.Close();
    //        return data;
    //    }
    //    else
    //    {
    //        Debug.LogError("파일이 존재하지 않습니다.");
    //    }

    //    return null;
    //}

#endif

    //.bin (프로그래머, .bin 파일로 데이터테이블 읽기)
    public static void LoadTable()
    {
        skillTable = ReadBinary<SkillData>("SkillData.bin");
        itemTable = ReadBinary<ItemData>("ItemData.bin");
        unitTalbe = ReadBinary<UnitData>("UnitData.bin");
        mapTable = ReadBinary<MapData>("MapData.bin");
    }

    public static List<T> ReadBinary<T>(string fileName) where T : struct, IDataSetter
    {
        string path = Application.dataPath + "/Resources/bin/" + fileName;

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open);
        List<T> table = (List<T>)formatter.Deserialize(stream);
        stream.Close();

        return table;
    }
    public static void WriteBinaryFiles()
    {
        string path = Application.dataPath + "/Resources/bin/";

        WriteBinary(path + "SkillData.bin", SkillTable);
        WriteBinary(path + "ItemData.bin", ItemTable);
        WriteBinary(path + "UnitData.bin", UnitTable);
        WriteBinary(path + "MapData.bin", MapTable);
    }
    private static void WriteBinary<T>(string path, List<T> table) where T : struct, IDataSetter
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, table);
        stream.Close();
    }
}