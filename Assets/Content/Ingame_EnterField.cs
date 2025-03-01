using Script.Data;
using Script.Index;
using Script.Manager;
using System.Threading.Tasks;

namespace Script.Content
{
    public class Ingame_EnterField : IngameLogicBase, IMessageReceiver
    {
        private enum State
        { 
            NONE = 0,

            LOAD_MAP_DATA,
            SET_MAP_INSTANCE,
            SET_PLYAER_INSTANCE,

            CLOSE
        }
        private State state;
        private int gridKey;

        private Task loadMapTask;

        public Ingame_EnterField(int targetGridKey)
        {
            ingameLogicType = IngameLogicIndex.ENTER_FIELD;

            state = State.LOAD_MAP_DATA;
            gridKey = targetGridKey;

            IngameManager.AddIngame(this);
        }

        public void Receive(Message_t msg)
        {
            switch (msg.Type)
            {
                case MessageType.GET_ASSET:
                    if (AssetIndex.DB_MAP_NAVI == msg.AssetIndex)
                    {
                        int instanceID = msg.ValueInt;
                        RawMapGridData rawData = AssetManager.GetCachedData<RawMapGridData>(instanceID);
                        IngameManager.TryAddMapRawGridData(rawData.gridKey, rawData);


                    }
                    break;
            }
        }


        public override IngameState MoveNext()
        {
            switch (state)
            {
                case State.LOAD_MAP_DATA:
                    loadMapTask = DataManager.ReadBinaryMappingDataAsync<RawMapGridData>(gridKey);
                    break;


                case State.CLOSE:
                    loadMapTask.Dispose();
                    loadMapTask = null;

                    // '탐험하기' 태스크를 생성한다? 이건 field manager에서 하는 게 좋을 듯?
                    return IngameState.SUCCESS;

                default:
                    return IngameState.FAILURE;
            }
            return IngameState.RUNNING;
        }
    }
}
