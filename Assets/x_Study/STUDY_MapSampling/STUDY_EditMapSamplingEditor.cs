#if UNITY_EDITOR
namespace Study.MapSampling
{
    using UnityEditor;
    using UnityEngine;

    public class STUDY_EditMapSamplingEditor
    {
        [MenuItem("Tools/MapSampling/Bake Path Nodes to .bin")]
        public static void Bake()
        {
            STUDY_EditMapSampling sampler = new STUDY_EditMapSampling();
            sampler.Bake();
        }

        [MenuItem("Tools/MapSampling/Load Baked Path Nodes")]
        public static async Awaitable TempLoad()
        {
            Debug.Log("Not yet developed");

           //for test
           //STUDY_NodeCacheManager cache = new STUDY_NodeCacheManager();
           //await cache.LoadFromAddressableAsync($"MapNavi_0");

           // foreach (var node in cache.NodeMap.Values)
           // {
           //     Debug.Log($"PATH[{node.ID}], {node.ComputeAbsPosition()}, link:{System.Convert.ToString(node.LinkMask, 2)}");
           // }
        }
    }
}
#endif