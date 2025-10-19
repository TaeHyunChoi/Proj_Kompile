namespace Script.Data
{
    using MessagePack;
    using Script.Manager;
    using System.Collections.Generic;

    [MessagePackObject]
    public class MapGridData
    {
        [Key(0)]
        [Unity.Collections.ReadOnly] public int gridKey;

        [Key(1)]
        [Unity.Collections.ReadOnly] public ConcurrentDictionary<int, MapTileData> MapNavDataDictionary;

        [Key(2)]
        [Unity.Collections.ReadOnly] public List<string> assetFiles;
        // 이걸 레이어 별로 꺼낼 방법이 있나? + 이름도 헷갈린다. layer_mesh_asset 등으로 바꾸는게 더 직관적일 듯
        // Dictionary<int layer_index, List<string> asset_file_list> 로 하면 편하려나? 자료 구조가 너무 복잡해지려나. 최대한 list로 맞추면 좋을 것 같긴 합니다만
        // 이럴거면 struct LayerMeshAsset { int index; string[] files } 만들어서 저장하는게 좋으려나?...
        // 잠깐만~ 불러오는게 문제가 아니라 .SetActive(on); 을 해야 하네~ 이걸 어떻게 하면 좋을까~
        // field manager에서 각 grid별 상태값을 관리/제어할 수 있어야 하네.
        // 그렇다면.. field manager에서 grid.mesh를 들고 있어야 함 -> 에셋별 layer_index가 필요함;
        // (1) TileData에는 int layer_mask 값 넣고
        // (2) fieldManager에서 그리드별 layer-mesh-asset을 들고 있어야 한다.

        [Key(3)]
        [Unity.Collections.ReadOnly] public List<GridLayerData> mesh;

        [IgnoreMember] // 런타임 중에만 사용한다.
        public int[] mesh_asset_instanceIDs;

        [IgnoreMember] // 런타임 중에만 사용한다.
        public UnityEngine.GameObject gameObject;

        public bool ContainTile(int tKey)
        {
            return MapNavDataDictionary.ContainsKey(tKey);
        }
        public bool TryGetTileData(int tileIntKey, out MapTileData tileData)
        {
            return MapNavDataDictionary.TryGetValue(tileIntKey, out tileData);
        }

        public void SetChildObjectMeshIDs(int[] ids)
        {
            mesh_asset_instanceIDs = ids;
        }
        public void Dispose()
        {
            for (int i = 0; i < mesh_asset_instanceIDs.Length; ++i)
            {
                AssetManager.Dispose(mesh_asset_instanceIDs[i]);
            }
        }

        // 이거 괜찮나?
        ~MapGridData()
        {
            Dispose();
        }
    }
}
