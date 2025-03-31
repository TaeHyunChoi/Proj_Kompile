using Script.Data;
using Script.Index;
using Script.Manager;
using System.Threading.Tasks;

namespace Script.Content
{
    public class NewGameHandler : _IngameHandlerBase, IMessageReceiver
    {
        private enum State
        { 
            NONE = 0,

            LOAD_MAP_DATA,
            SET_MAP_INSTANCE,
            SET_PLYAER_INSTANCE,

            CLOSE
        }

        private Task loadMapTask;
        private State state;
        private int gridKey;

        public NewGameHandler(int targetGridKey)
        {
            handlerType = IngameHandlerType.ENTER_FIELD;

            state = State.LOAD_MAP_DATA;
            gridKey = targetGridKey;

            IngameManager.AddIngameHandler(this);
            MoveNext();
        }

        public bool Receive<T>(MessageType type, T data) where T : struct
        {
            if (type == MessageType.GET_ASSET
                && data is OnGetAsset_MapGridData getRawMapGridData)
            {
                AssetCode code = getRawMapGridData.AssetCode;
                MapGridData grid = getRawMapGridData.Data;

                loadMapTask.Dispose();
                loadMapTask = null;

                state = State.CLOSE;

                return true;
            }

            return false;
        }

        public override IngameHandlerState MoveNext()
        {
            switch (state)
            {
                case State.LOAD_MAP_DATA:
                    //loadMapTask = DataManager.ReadBinaryRawMapGridDataAsync(gridKey);
                    break;


                case State.CLOSE:

                    // '탐험하기' 태스크를 생성한다? 이건 field manager에서 하는 게 좋을 듯?
                    return IngameHandlerState.SUCCESS;

                default:
                    return IngameHandlerState.FAILURE;
            }
            return IngameHandlerState.RUNNING;
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
