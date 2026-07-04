#if UNITY_EDITOR
namespace Kompile.Editor.Provider
{
    using Kompile.Editor.Data;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public partial class EditMapSamplingProvider
    {
        private class EditBakeContext
        {
            public int SceneIndex;
            public ConcurrentDictionary<int, EditMapGridData> Map;
            public List<(string path, string assetName)> CreatedAssets;
            public string AddressableGroupName;

            public EditBakeContext()
            {
                SceneIndex = 0;
                Map = null;
                CreatedAssets = new List<(string path, string assetName)>();
                AddressableGroupName = null;
            }

            public void Setup(int sceneIndex, ConcurrentDictionary<int, EditMapGridData> map, string groupName)
            {
                SceneIndex = sceneIndex;
                Map = map;
                CreatedAssets.Clear();
                AddressableGroupName = groupName;
            }
        }
    }
}
#endif