#if UNITY_EDITOR
namespace Study.Pathfind
{
    using Script.Editor.MapSampling;
    using MessagePack;
    using MessagePack.Resolvers;
    using Script.Data;
    using Script.Manager;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;

    public class STUDY_EditPathNodeBaker
    {
        public static int GRID_SIZE = STUDY_PositionKeyUtil.GRID_SIZE;

        [MenuItem("Tools/Pathfinding/Bake Path Nodes to .bin")]
        public static void Bake()
        {
            EditMapTileSampling sampler = Object.FindFirstObjectByType<EditMapTileSampling>();
            sampler.Bake();

            ConcurrentDictionary<int, EditMapGridData> map = sampler.Map;

            List<STUDY_NodeData> list = new List<STUDY_NodeData>(map.Keys.Count);
            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            byte[] bytes;

            foreach (var gKV in map)
            {
                list.Clear();

                int gKey = gKV.Key;
                EditMapGridData grid = gKV.Value;

                foreach (var tKV in grid.Data)
                {
                    int tKey = tKV.Key;
                    EditMapTileData tile = tKV.Value;

                    list.Add(new STUDY_NodeData {
                        ID = STUDY_PositionKeyUtil.ComputeID(gKey, tKey),
                        LinkMask = tile.LinkMask,
                    });
                }

                int3 gAbsPivot = STUDY_PositionKeyUtil.ComputeAbsoluteGridPivot(gKey);
                bytes = MessagePackSerializer.Serialize(list, options);

                AssetManager.WriteBinaryFile<List<STUDY_NodeData>>(
                    data            : list,
                    dataPath        : "Rcs\\Bin\\MapNodeData",
                    fileName        : $"path_nodes_{gKey}",
                    addressableGroup: "MapPath"
                    );
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Pathfinding/[TEST]Load Baked Path Nodes")]
        public static async Awaitable TempLoad()
        {
            // for test
            STUDY_NodeCacheManager cache = new STUDY_NodeCacheManager();
            await cache.LoadFromAddressableAsync($"path_nodes_0");

            foreach (var node in cache.NodeMap.Values)
            {
                Debug.Log($"PATH[{node.ID}], {node.ComputeAbsPosition()}, link:{System.Convert.ToString(node.LinkMask, 2)}");
            }
        }
    }
}
#endif