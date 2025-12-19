#if UNITY_EDITOR
namespace Study.Pathfind
{
    using MapSampling;
    using MessagePack;
    using MessagePack.Resolvers;
    using Script.Data;
    using Script.Manager;
    using System.Collections.Generic;
    using System.IO;
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
            sampler.Save();

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
    }
}
#endif