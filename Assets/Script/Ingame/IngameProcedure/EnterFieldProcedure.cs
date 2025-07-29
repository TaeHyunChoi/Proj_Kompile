namespace Script.Manager
{
    using Script.Content;
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using System;
    using System.Threading.Tasks;
    using UnityEngine;

    public class EnterFieldProcedure : IngameProcedureBase
    {
        // 이거를 모두 FieldManager.cs 로 넘기는 게 맞는 거 같은데?
        // private ConcurrentDictionary<int, MapGridData> mapGridData;
        // player character units
        // private IngameFieldPlayer[] players;


        public override async Task<bool> Start()
        {
            // get player data - 여기 들어오기 전에 먼저 가져왔다고 치고...
            PlayData playData = IngameManager.GetPlayData();

            // MapGridData를 어떻게 관리하면 좋을까요?...
            MapGridData targetData = await AssetManager.Temp(playData.Grid);
            //MapGridData targetData = await AssetManager.LoadMapGridBinaryData(gridKey);
            //bool result = mapGridData.TryAdd(gridKey, targetData); //뭐야 그럼 해제는 어떻게 함? 아이고..
            //result = await AssetManager.InstaniateMapGrid(mapGridData[gridKey]);


            // set playable unit
            GameObject obj = await AssetManager.GetOrNewInstanceAsync(AssetCode.UnitBase, AssetParentType.UNIT_ROOT);
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

            MessageManager.Publish(new OnEndEvent(IngameEventType.FIELD_INIT));
            return true;
        }

        protected override Task<bool> ExecuteIngameEventAsync(IngameEventType messageType)
        {
            throw new NotImplementedException();
        }
    }
}
 