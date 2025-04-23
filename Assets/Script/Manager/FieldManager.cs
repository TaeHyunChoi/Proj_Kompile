namespace Script.Manager
{
    using Script.Data;
    using System;
    using System.Threading.Tasks;
    using UnityEngine;

    public class FieldManager
    {
        private ConcurrentDictionary<int, MapGridData> mapGridData;

        // player character units
        private IngameFieldPlayer[] players;

        // npcs



        public FieldManager()
        {
            mapGridData = new ConcurrentDictionary<int, MapGridData>();
            players     = new IngameFieldPlayer[3];
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
            GameObject obj = await AssetManager.InstantiateIngameObjectAsync<GameObject>(Script.Index.AssetCode.UnitBase, true);
            IngameFieldPlayer player_character = obj.AddComponent<IngameFieldPlayer>();
            Debug.Assert(null != player_character, "player character null");

            // 플레이어 캐릭터에게 무엇을 전달해야 할까요?
            player_character.transform.position = new Vector3(1f, 0f, 1f);

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
