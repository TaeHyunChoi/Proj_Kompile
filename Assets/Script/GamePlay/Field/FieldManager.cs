namespace Script.GamePlay
{
    using Script.Data;
    using Script.Map;
    using UnityEngine;

    public class FieldManager : ManagerBase
    {
        private PlayData playData;



        public FieldManager(PlayData playData)
        {
            this.playData = playData;
        }

        public override Awaitable Intialize()
        {
            // var task_map = Init-Map
            // var task_unit = Init-Unit
            // var task_hud = UI.HUD
            // await Task.WaitAll(task_map, task_unit, task_hud);
            return null;
        }
        private async Awaitable InitializeMap()
        {
            MapPathUtil.ComputeKey(playData.Position, out int gridKey, out int tileKey);

            // 근데 이거 asset system으로 불러오는게 맞지 않니?
            // (1) navi data 불러오기
            // (2) map object 불러오기
            // 둘 다 FieldManager에서 관리하도록 가져와야 하네?



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

        }
    }
}