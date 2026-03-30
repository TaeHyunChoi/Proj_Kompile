#if UNITY_EDITOR
// [Framework] 규칙에 따라 단수형 Script를 복수형 Scripts로, Data를 Datas로 변경
namespace Script.Map.Data
{
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using MessagePack;

    // Data: 정보의 상태, 구조, 가공 형태 및 자료형 정의 (Value-Centric)
    [MessagePackObject]
    public class EditMapGridData
    {
        [Key(0)]
        public int gridKey;

        [Key(1)]
        public ConcurrentDictionary<int, EditMapTileData> Data;

        [Key(2)]
        public List<string> assetFiles;

        [Key(3)]
        public List<MapGridLayerData> LayerMeshAssets = new List<MapGridLayerData>();

        // MessagePack 역직렬화 시 사용할 기본 생성자를 명확히 지정
        [SerializationConstructor]
        public EditMapGridData() { }

        public EditMapGridData(int targetGridKey)
        {
            gridKey = targetGridKey;
            Data = new ConcurrentDictionary<int, EditMapTileData>();
            assetFiles = new List<string>();
        }

        public ConcurrentDictionary<int, MapTileData> ParseData()
        {
            ConcurrentDictionary<int, MapTileData> data = new ConcurrentDictionary<int, MapTileData>();

            foreach (var kvp in Data)
            {
                data.TryAdd(kvp.Key, new MapTileData(kvp.Value));
            }

            return data;
        }

        public void AddAssetFile(string fileName)
        {
            assetFiles.Add(fileName);
        }

        public bool TryAdd(int key, EditMapTileData navData)
        {
            return Data.TryAdd(key, navData);
        }

        public void AddMeshAsset(int layer, string fileName)
        {
            for (int i = 0; i < LayerMeshAssets.Count; ++i)
            {
                if (layer == LayerMeshAssets[i].layer)
                {
                    LayerMeshAssets[i].Add(fileName);
                    return;
                }
            }

            LayerMeshAssets.Add(new MapGridLayerData(layer, fileName));
        }
    }
}
#endif