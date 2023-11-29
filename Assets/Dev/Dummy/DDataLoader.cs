using System.Collections;
using System.Threading;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [목표] CSV 데이터 불러오기 최적화 (속도 향상을 위하여 안정성은 상대적으로 낮춤)
/// [방법] (1)기존 string → byte[] 로 읽어오기 (2)Generic<T> 활용하여 코드 재사용성 높이기
/// [비고] Job System을 사용하여 멀티 스레딩으로 CSV를 불러오려 했으나 'reference type은 Job struct에서 사용할 수 없다'는 제한이 있어 기각.
/// </summary>
public class DDataLoader
{
    //테이블은 '단 하나'이다.
    private static List<SkillData> SkillTBL;

    //public static List<T> ParseCSVToData<T>(string fileName) where T : DInterface.IDataSetter, new()
    //{
    //    List<T> table = new List<T>();

    //    //csv 파일을 전부 읽었고..
    //    //bytes로 읽으니까 데이터 변환 쪽에서 이슈가 생기는구나. 확실히 빠른 것 같은데 흡...
    //    //토큰만 잘 나누면 어떻게든 될 것 같다...?
    //    //그런데 결국 text로 변환하여 다시 읽는거라 문제네. 데이터 자체가 텍스트 기반. 흠...
    //    //구글 스프레드시트에서 .bytes로 저장하면 숫자단위로 저장해주려나?

    //    string path = Application.dataPath + "/Resources/CSV/" + fileName + ".csv";
    //    Debug.Log(path);

    //    byte[] raw = File.ReadAllBytes(path);

    //    int lastIndex, curIndex = 0;

    //    while (raw[curIndex++] != (byte)'\n')
    //    {
    //        //Do Nothing; 첫 줄 라벨을 날린다.
    //    }
    //    lastIndex = curIndex;

    //    //T data;
    //    string temp = string.Empty;

    //    while (curIndex < raw.Length)
    //    {
    //        if (raw[curIndex] == (byte)'\n') //한 줄씩 읽었고
    //        {
    //            for (int i = lastIndex; i < curIndex; ++i)
    //            {
    //                temp += raw[i];
    //            }
    //            Debug.Log($"{lastIndex} ~ {curIndex} : {temp}");
    //            lastIndex = curIndex + 1;
    //        }

    //        ++curIndex;
    //    }

    //    temp = string.Empty;
    //    for (int i = lastIndex; i < curIndex; ++i)
    //    {
    //        temp += (char)raw[i];
    //    }
    //    Debug.Log($"{lastIndex} ~ {curIndex} : {temp}");

    //    return table;
    //}
}
