namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using System.Threading.Tasks;
    using UnityEngine;

    public class FieldManager
    {
        private MapGridData currentMapGrid; // 일단 하나만 올려보자.
        private IngameFieldPlayer[] player_character = new IngameFieldPlayer[3];


        public async Task<bool> Initialize(PlayData playData)
        {
            // instantiage map
            currentMapGrid = await AssetManager.InstaniateMapGrid(playData.Grid);

            // instantiage player unit
            GameObject obj = await AssetManager.GetOrNewInstanceAsync(AssetCode.UnitBase, AssetParentType.UNIT_ROOT);

            // TODO: 테스트 목적이라서 나중에 다시 만들어야 함.
            player_character[0] = obj.AddComponent<IngameFieldPlayer>();
            IngameFieldPlayer player = player_character[0];

            if (true == await player.Init(0))
            {
                player.transform.position = new Vector3(1f, 0f, 1f);
                IngameManager.InitFollowingCamera(player);
            }
            else
            {
                Debug.Assert(false, "[TEST] Fail to initialize player_character");
                return false;
            }

            MessageManager.Publish(new OnEndEvent(IngameEventType.FIELD_INIT));
            return true;
        }
    }
}