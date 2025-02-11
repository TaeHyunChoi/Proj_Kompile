namespace Script.Content
{
    using System.Threading.Tasks;
    using Script.Manager;
    using Script.Index;
    using static Index.Index;
    using UnityEngine;

    public partial class Ingame_Opening
    {
        private enum State
        {
            NONE = 0,
            
            INSTANTIATE_PRF_OPENING,
            PLAY_OPENING, // Opeing 오브젝트 조작  - 종료 대기

            INSTANTIATE_UI_TITLE_MENU,
            SELECT_MENU,  // UITitle 오브젝트 조작 - 종료 대기

            END           // 타이틀 화면 뿌수기
        }
    }
    
    public partial class Ingame_Opening : IngameLogicBase, IMessageReceiver
    {
        private State state;
        private Task<GameObject> loadTask;


        public Ingame_Opening()
        {
            state = State.NONE;
            ingameLogicType = IngameLogicIndex.OPENING;
            MessageManager.AddReceiver(this);
            IngameManager.AddIngame(this);
        }

        public void Receive(Message_t msg)
        {
            int assetIndex = msg.GetIndex();

            switch (msg.GetMessageType())
            {
                case MessageType.GET_ASSET:
                    if (EAssetName.OpeningGame.ToInt() == assetIndex)
                    {
                        state = State.PLAY_OPENING;
                        Run();
                    }
                    if (EAssetName.UITitle.ToInt() == assetIndex)
                    { 
                        // 상태값 전환(2)
                    }
                    break;
                case MessageType.END_OBJECT_PROCESS:
                    if (EAssetName.OpeningGame.ToInt() == assetIndex)
                    {
                        state = State.INSTANTIATE_UI_TITLE_MENU;
                        Run();
                    }
                    break;
                default:
                    return;
            }
        }

        public override IngameState Run()
        {
            switch (state)
            {
                case State.NONE:
                    ++state;
                    goto case State.INSTANTIATE_PRF_OPENING;

                case State.INSTANTIATE_PRF_OPENING:
                    Transform parent = AssetManager.GetCanvas(CanvasType.OVERLAY).transform;
                    loadTask = AssetManager.GetGameObjectAssetAsync(EAssetName.OpeningGame, parent, true);
                    break;
                case State.PLAY_OPENING:
                    OP_PlayTitleAnime opening = loadTask.Result.GetComponent<OP_PlayTitleAnime>();
                    opening.MoveNext();
                    // next: opening에서 MoveNext() 호출하면
                    // 초기화 시점에서 IngameUpdater에 등록해야 함
                    // IngameUpdaterBase.cs 와 ITaskUpdater의 개념이 헷갈리는데요?
                    // 이참에 둘을 아예 분리해야 한다.
                    loadTask.Dispose();
                    break;

                case State.INSTANTIATE_UI_TITLE_MENU:
                    break;
                case State.SELECT_MENU:
                    break;

                case State.END:
                    return IngameState.SUCCESS;

                default:
                    return IngameState.FAILURE;
            }

            return IngameState.RUNNING;
        }
    }
}
