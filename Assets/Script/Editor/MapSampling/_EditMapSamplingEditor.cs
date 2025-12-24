#if UNITY_EDITOR
using MessagePack;
using MessagePack.Resolvers;
using Script.Data;
using Script.Map;
using System.Collections.Generic;
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
    public static async void EditLoadAll()
    {
        string label = "MapNavi";
        var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
        {
            // 칵 파일이 로드될 때마다 실행되는 콜백 (병렬 실행)
            if (null != textAsset)
            {
                var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);
                Debug.Log($"[Load Baked Map ] {textAsset.name}");
            }
        });
        await handle.Task;

        //MapCacheManager cacheMgr = new MapCacheManager();
        //await cacheMgr.EditLoadAll();
    }
}
#endif