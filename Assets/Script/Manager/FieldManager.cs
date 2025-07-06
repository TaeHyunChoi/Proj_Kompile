namespace Script.Manager
{
    using Script.Content;
    using Script.Data;
    using Script.Index;
    using System;
    using System.Threading.Tasks;
    using UnityEngine;

    public class FieldManager : _IngameHandlerBase
    {
        private ConcurrentDictionary<int, MapGridData> mapGridData;

        // player character units
        private IngameFieldPlayer[] players;

        // npcs



        public FieldManager() : base()
        {
            mapGridData = new ConcurrentDictionary<int, MapGridData>();
            players     = new IngameFieldPlayer[3];
        }

        protected override void ExecuteIngameEventAsync(IngameEventType messageType)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Init(PlayData playerData)
        {
            // get grid data
            int gridKey = playerData.Grid;
            MapGridData targetData = await AssetManager.LoadMapGridData(gridKey);
            bool result = mapGridData.TryAdd(gridKey, targetData);
            if (false == result)
            {
                throw new Exception("fail to add map grid data;");
            }

            // instantiate grid objects
            result = await AssetManager.InstaniateMapGrid(mapGridData[gridKey]);
            if (false == result)
            {
                throw new Exception("fail to instantiate map grid objects;");
            }

            // instantiate player character :: parent를 지정해줘야하는구나?
            //IngameAsset_t asset = await AssetManager.InstantiateGameObjectAsync(AssetCode.UnitBase, true);
            //assets.Add(asset);
            GameObject obj = await AssetManager.CreateInstanceAsync(AssetCode.UnitBase);


            //IngameFieldPlayer player_character = asset.AddComponent<IngameFieldPlayer>();
            IngameFieldPlayer player_character = obj.AddComponent<IngameFieldPlayer>();
            bool isInit = await player_character.Init();

            // 플레이어 캐릭터에게 무엇을 전달해야 할까요?
            player_character.transform.position = new Vector3(1f, 0f, 1f);

            // 애니메이션을 어떻게 쥐어주면 좋을까?
            // 유닛마다 여러 개의 애니메이션을 들고 있고..
            // 어드레서블 에셋으로 관리해서 메모리 관리를 하고 싶은걸까?
            // 그냥 animation controller 쥐어주는 게 좋을 것 같은데?

            // 플레이어 캐릭터 생성하기..
            // player data position도 가져와야 하고
            // 일단 패스

            // set input

            // init camera
            IngameManager.InitFollowingCamera(player_character);

            return true;
        }
    }
}
