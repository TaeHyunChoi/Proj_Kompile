using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public class EntryToProcess
{
    public string EnumName;       // enum 자료형 이름
    public string UnityAssetPath; // [Directory] Assets/를 포함한 전체 경로
}

public static class AssetMapGenerator
{
    //GenerateMap() 참고
    private const string ASSET_MAP_PATH = "Assets/GameData/AddrAssetMap.asset";

    public static List<EntryToProcess> EntrieToProcess = new List<EntryToProcess>();

    private static bool TryGetAddress(string assetPath, out string address)
    {
        address = null;

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (true == string.IsNullOrEmpty(guid))
        {
            return false;
        }

        // 이게 뭐야;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (null == settings)
        {
            Debug.LogError("Addressables Settings를 찾을 수 없습니다.");
            return false;
        }

        // 이건 또 뭐여 22
        AddressableAssetEntry entry = settings.FindAssetEntry(guid);
        if (null == entry)
        {
            return false;
        }

        address = entry.address;
        return true;
    }

    // 매핑 실행 함수 (delayCall에서 호출)
    public static void GenerateMap(List<EntryToProcess> entries)
    {
        string enumName;
        Type enumType;
        for (int i = 0; i < entries.Count; ++i)
        {
            enumName = entries[i].EnumName;
            enumType = Type.GetType(enumName);
        }

        // 여기서부터
    }
}
