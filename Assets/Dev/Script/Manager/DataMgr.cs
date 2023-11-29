using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class DataMgr
{
    //## DataTable
    public static List<SkillData> SkillTBL { get; private set; }
    public static List<ItemData> ItemTBL { get; private set; }
    public static List<UnitData> UnitTBL { get; private set; }
    public static List<MapData> MapTBL { get; private set; }

    //.csv (기획자, 에디터에서 .bin 파일로 변환 필요)
    public static void LoadCSVTable()
    {
        SkillTBL = LoadTable<SkillData>("SkillData");
        ItemTBL  = LoadTable<ItemData> ("ItemData");
        UnitTBL  = LoadTable<UnitData> ("UnitData");
        MapTBL   = LoadTable<MapData>  ("MapData");
    }
    private static List<T> LoadTable<T>(string fileName) where T : Interface.IDataSetter, new()
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

    //.bin (프로그래머, .bin 파일로 데이터테이블 읽기)
    public static void LoadTable()
    {
        SkillTBL = ReadBinary<SkillData>("SkillData.bin");
        ItemTBL  = ReadBinary<ItemData>("ItemData.bin");
        UnitTBL  = ReadBinary<UnitData>("UnitData.bin");
        MapTBL   = ReadBinary<MapData>("MapData.bin");
    }
    public static void WriteBinaryFiles()
    {
        string path = Application.dataPath + "/Resources/bin/";

        WriteBinary(path + "SkillData.bin", SkillTBL);
        WriteBinary(path + "ItemData.bin",  ItemTBL);
        WriteBinary(path + "UnitData.bin",  UnitTBL);
        WriteBinary(path + "MapData.bin",   MapTBL);
    }
    private static void WriteBinary<T>(string path, List<T> table) where T: struct, Interface.IDataSetter
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream, table);
        stream.Close();
    }
    public static List<T> ReadBinary<T>(string fileName) where T : struct, Interface.IDataSetter
    {
        string path = Application.dataPath + "/Resources/bin/" + fileName;

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open);
        List<T> table = (List<T>)formatter.Deserialize(stream);
        stream.Close();

        return table;
    }
}