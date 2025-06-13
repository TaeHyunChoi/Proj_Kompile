namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Interface;
    using Script.Data;
    using System.Threading.Tasks;

    public class IngameManager : MonoBehaviour
    {
        private const int RESET_UPDATER_LIST_COUNT = 10;

        private static PlayData         playerData;

        private static IngameManager    instance;
        private static InputManager     inputMgr;
        private static FieldManager     fieldMgr;   // 사실 얘도 IngameHandler로 빠져야 함.
        private static IngameCamera     ingameCam;

        private static List<_IngameHandlerBase> ingameHandler;
        private static int targetHandlerIndex;

        private static List<IIngameUpdater>      updateList;
        private static List<IIngameFixedUpdater> fixedUpdateList;
        private static List<IIngameLateUpdater>  lateUpdateList;

        public static void AddUpdater(IIngameUpdater updater)
        {
            updateList.Add(updater);
        }
        public static void AddFixedUpdater(IIngameFixedUpdater fixedUpdater)
        {
            fixedUpdateList.Add(fixedUpdater);
        }
        public static void AddLateUpdater(IIngameLateUpdater lateUpdater)
        {
            lateUpdateList.Add(lateUpdater);
        }

        public static void RemoveUpdater(IIngameUpdater updater)
        {
            updateList.Remove(updater);
        }
        public static void RemoveFixedUpdater(IIngameFixedUpdater fixedUpdater)
        {
            fixedUpdateList.Remove(fixedUpdater);
        }
        public static void RemoveLateUpdater(IIngameLateUpdater lateUpdater)
        {
            lateUpdateList.Remove(lateUpdater);
        }


        // play data
        public static void InitPlayData()
        {
            playerData = new PlayData();

#if UNITY_EDITOR
            Debug.Log("NEWGAME_PLAYER_DATA");
#endif
        }


        // manager
        public static async Task<bool> TryInitializeField()
        {
            fieldMgr = new FieldManager();
            bool result = await fieldMgr.Init(playerData);

            // 여기서 캠 설정도 넣는다?
            //MessageManager.Publish(IngameEventType.END_OBJECT_PROCESS,)
            return result;
        }


        // ingame_handler
        public static void AddIngameHander(IngameHandlerType type)
        {
            _IngameHandlerBase handler;
            switch (type)
            {
                case IngameHandlerType.OPENING:     handler = new OpeningHandler(); break;
                case IngameHandlerType.NEW_GAME:    handler = new NewGameHandler(); break;
                default:
                    return;
            }

            ingameHandler.Add(handler);
            targetHandlerIndex += 1;
        }
        public static void RemoveIngameHandler(IngameHandlerType type)
        {
            for (int i = ingameHandler.Count - 1; i >= 0; --i)
            {
                if (type == ingameHandler[i].HandlerType)
                {
                    targetHandlerIndex -= 1;
                    ingameHandler[i].Dispose();
                    ingameHandler.RemoveAt(i);
                    return;
                }
            }
        }

        // camera
        public static void InitFollowingCamera(_IngameUnitBase player_character)
        {
            ingameCam.InitFollowingCamera(player_character);
        }


        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            updateList      = new List<IIngameUpdater>();
            fixedUpdateList = new List<IIngameFixedUpdater>();
            lateUpdateList  = new List<IIngameLateUpdater>();


            AssetManager.Initialize(this.transform);

            inputMgr = new InputManager();
            fieldMgr = new FieldManager();

            ingameHandler = new List<_IngameHandlerBase>();
            targetHandlerIndex = -1;

            ingameCam = transform.GetComponentInChildren<IngameCamera>(true);
        }
        private void Start()
        {
            AddIngameHander(IngameHandlerType.OPENING);
        }
        
        private void Update()
        {
            // update: input
            if (IngameUpdateState.FAILURE == inputMgr.UpdateState())
            {
                //Error
                return;
            }

            for (int i = 0; i < updateList.Count; ++i)
            {
                switch (updateList[i].UpdateState())
                {
                    case IngameUpdateState.SUCCESS:
                        updateList.RemoveAt(i--);
                        break;
                    case IngameUpdateState.FAILURE:
#if UNITY_EDITOR
                        Debug.Assert(false, $"Updater[{i}].State == FAILTURE;");
#endif
                        break;
                    default:
                        // Running
                        break;
                }
            }
        }
        private void FixedUpdate() 
        {
            for (int i = 0; i < fixedUpdateList.Count; ++i)
            {
                switch (fixedUpdateList[i].FixedUpdateState())
                {
                    case IngameUpdateState.SUCCESS:
                        fixedUpdateList.RemoveAt(i--);
                        break;
                    case IngameUpdateState.FAILURE:
#if UNITY_EDITOR
                        Debug.Assert(false, $"FixedUpdater[{i}].State == FAILTURE;");
#endif
                        break;
                    default:
                        // Running
                        break;
                }
            }
        }
        private void LateUpdate()  
        {
            for (int i = 0; i < lateUpdateList.Count; ++i)
            {
                switch (lateUpdateList[i].LateUpdateState())
                {
                    case IngameUpdateState.SUCCESS:
                        lateUpdateList.RemoveAt(i--);
                        break;
                    case IngameUpdateState.FAILURE:
#if UNITY_EDITOR
                        Debug.Assert(false, $"LateUpdater[{i}].State == FAILTURE;");
#endif
                        break;
                    default:
                        // Running
                        break;
                }
            }
        }

        // Queue처럼 사용하려고 했나?...
        public static void GetInput(IDxInput.InputFlag inputFlag)
        {
            if (targetHandlerIndex < 0)
            {
                return;
            }

            ingameHandler[targetHandlerIndex].ReceiveIngameInput(inputFlag);
        }

        private void OnEnable()
        {
            inputMgr.OnEnable();
        }
        private void OnDisable()
        {
            inputMgr.OnDisable();
        }
    }
}