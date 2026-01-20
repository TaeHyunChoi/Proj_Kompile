namespace Script.GamePlay
{
    using Script.Asset;
    using Script.Data;
    using Script.Map;
    using Script.GameSystem.Utility;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    public class FieldManager : ManagerBase
    {
        private PlayData playData;
        private MapGridObject mapGridObject;

        public FieldManager(PlayData playData)
        {
            this.playData = playData;
        }

        public override async Awaitable Intialize()
        {
            Awaitable task_map = InitializeMap();
            Awaitable task_unit = InitializeUnit();
            Awaitable task_ui = InitializeUI();

            await task_map;
            await task_unit;
            await task_ui;
        }
        private async Awaitable InitializeMap()
        {
            MapPathUtil.ComputeKey(playData.Position, out int gridKey, out int tileKey);

            // (1) navi data 불러오기
            MapGridData data = await LoadMap(gridKey);
            if (null == data)
            {
                // error
                return;
            }

            // (2) map object 불러오기
            List<(int, Mesh)> meshes = new List<(int, Mesh)>();
            Mesh mesh;
            string address;
            for (int i = 0; i < data.layerMeshAssets.Count; ++i)
            {
                var layerData = data.layerMeshAssets[i];
                int index = layerData.layer;

                for (int l = 0; l < layerData.assets.Count; ++l)
                {
                    address = layerData.assets[i];
                    mesh = await AssetSystem.LoadAssetAsync<Mesh>(address); //해제할 때엔 mesh.GetInstanceID()로

                    meshes.Add((index, mesh));
                }
            }

            var prefabObj = await AssetSystem.GetOrNewInstanceAsync(PrefabID.MapGridPrefab, usePooling: true);
            mapGridObject = prefabObj.GetComponent<MapGridObject>();
            mapGridObject.Initialize(meshes);
        }
        private async Awaitable InitializeUnit()
        { 
        
        }
        private async Awaitable InitializeUI()
        { 
        
        }

        public override bool OnInputReceive(DataType.InputState inputState)
        {
            return false;
        }

        public override bool OnUpdate()
        {
            return false;
        }
        public override void Dispose()
        {
            mapGridObject.Dispose();
            AssetSystem.ReleaseInstance(mapGridObject);
        }

        private async Awaitable<MapGridData> LoadMap(int gridKey)
        {
            string label = $"MapNavi_{gridKey}";
            MapGridData grid = null;

            var handle = Addressables.LoadAssetsAsync<TextAsset>(label, callback: (textAsset) =>
            {
                if (null != textAsset)
                {
                    grid = GameDataSerializer.Deserialize<MapGridData>(textAsset.bytes);
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

            return grid;
        }
    }
}