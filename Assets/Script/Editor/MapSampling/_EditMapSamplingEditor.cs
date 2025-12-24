#if UNITY_EDITOR
using System.Collections.Generic;
using Script.Data;
using Script.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class STUDY_EditMapSamplingEditor
{
    [MenuItem("Tools/MapSampling/Bake Path Tiles to .btyes")]
    public static void Bake()
    {
        EditMapSampling sampler = new EditMapSampling();
        sampler.Bake();
    }

    [MenuItem("Tools/MapSampling/Load Baked Map Tiles")]
    public static async Awaitable EditLoadAll()
    {
        // Addressable Label로 모든 MapNavi.bytes 불러오기
        string label = "MapNavi";
        var locations = await Addressables.LoadResourceLocationsAsync(label).Task;
        if (null == locations || 0 == locations.Count)
        {
            Debug.Log($"'{label}'로 지정된 파일을 찾을 수 없습니다.");
            return;
        }

        MapCacheManager cacheMgr = new MapCacheManager();
        foreach (var location in locations)
        {
            await cacheMgr.LoadFromAddressableAsync(location.PrimaryKey);
            foreach (KeyValuePair<long, MapTileData> tileKV in cacheMgr.TileDic)
            {
                var id = tileKV.Key;
                var tile = tileKV.Value;
                var pivot = MapPathUtil.ComputeWorldPosition(id);

                Debug.Log($"[{id}] {pivot}.navi = {System.Convert.ToString(tile.NaviMask, 16)},\nlink = {System.Convert.ToString(tile.LinkMask, 2)}");
            }
        }
    }
}
#endif