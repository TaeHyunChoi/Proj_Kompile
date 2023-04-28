using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class DataMgr
{
    //## DataTable
    public static List<SkillData> SkillTBL { get; private set; }
    public static List<ItemData> ItemTBL { get; private set; }
    public static List<UnitData> UnitTBL { get; private set; }
    public static List<MapData> MapTBL { get; private set; }

    public static void LoadCSVTable()
    {
        SkillTBL = LoadTable<SkillData>(ReadCSVFile("SkillData"));
        ItemTBL = LoadTable<ItemData>(ReadCSVFile("ItemData"));
        UnitTBL = LoadTable<UnitData>(ReadCSVFile("UnitData"));
        MapTBL = LoadTable<MapData>(ReadCSVFile("MapData"));
    }


    private static List<Dictionary<string, string>> ReadCSVFile(string fileName)
    {
        List<Dictionary<string, string>> table = new List<Dictionary<string, string>>();
        TextAsset     csv = Resources.Load<TextAsset>("CSV/" + fileName);
        StringReader  reader = new StringReader(csv.text);
        StringBuilder sb = new StringBuilder();

        //Setting
        string[] columns;   //칼럼명
        int index = -1;     //칼럼명[] 인덱스
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

        return table;
    }
    private static List<T> LoadTable<T>(List<Dictionary<string, string>> table) where T : Interface.IDataSetter, new()
    {
        List<T> list = new List<T>();
        for (int i = 0; i < table.Count; ++i)
        {
            T tData = new T();
            tData.SetTable(table[i]);
            list.Add(tData);
        }

        return list;
    }
}