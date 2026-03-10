#if UNITY_EDITOR
using MessagePack;
using MessagePack.Resolvers;
using Script.Data;
using Script.Map.Provider;
using Script.Map.Utility;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class EditMapSamplingEditor
{
    [MenuItem("Tools/MapSampling/Bake Path Tiles to .btyes")]
    public static void Bake()
    {
        EditMapSamplingRepoProvider sampler = new EditMapSamplingRepoProvider();
        sampler.Bake();
    }

    [MenuItem("Tools/MapSampling/Load Baked Map Tiles")]
    public static async void EditLoadAll()
    {
        string label = "MapNavi";
        var handle = Addressables.LoadAssetsAsync<TextAsset>(label, (textAsset) =>
        {
            // 칵 파일이 로드될 때마다 실행되는 콜백 (병렬 실행)
            if (null != textAsset)
            {
                var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
                MapGridData grid = MessagePackSerializer.Deserialize<MapGridData>(textAsset.bytes, options);
                Debug.Log($"[Load Baked Map ] {textAsset.name}");

                int gKey = grid.Key;
                foreach (var tKV in grid.NaviTileDict)
                {
                    int tKey = tKV.Key;
                    var tile = tKV.Value;

                    MapCoordUtil.ComputeID(gKey, tKey, out long id);

                    MapCoordUtil.ComputeWorldPosition(id, out float3 pos);
                    Debug.Log($"{pos} nav:{System.Convert.ToString(tile.NaviMask, 16)}, link:{System.Convert.ToString(tile.LinkMask, 2)}");

                }

            }
        });

        try
        {
            await handle.Task;
        }
        finally
        {
            if (true == handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        //MapCacheManager cacheMgr = new MapCacheManager();
        //await cacheMgr.EditLoadAll();
    }
}
#endif