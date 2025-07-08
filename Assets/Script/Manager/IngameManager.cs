namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Data;
    using System.Threading.Tasks;

    public partial class IngameManager : MonoBehaviour
    {
        private static IngameManager    instance;

        private static PlayData         playerData;

        private static InputManager     inputMgr;
        //private static x_FieldManager     fieldMgr;   // 사실 얘도 IngameHandler로 빠져야 함.
        private static IngameCamera     ingameCam;

        private static List<IngameProcedureBase> ingameProcedures;

        // play data
        public static void InitPlayData()
        {
            playerData = new PlayData();

#if UNITY_EDITOR
            Debug.Log("NEWGAME_PLAYER_DATA");
#endif
        }


        // manager
        //public static async Task<bool> TryInitializeField()
        //{
        //    fieldMgr = new x_FieldManager();
        //    bool result = await fieldMgr.Init(playerData);

        //    // 여기서 캠 설정도 넣는다?
        //    //MessageManager.Publish(IngameEventType.END_OBJECT_PROCESS,)
        //    return result;
        //}


        // ingame_handler
        public static void AddIngameHander(IngameProcedureType type)
        {
            IngameProcedureBase handler;
            switch (type)
            {
                case IngameProcedureType.OPENING:     handler = new OpeningProcedure(); break;
                case IngameProcedureType.NEW_GAME:    handler = new NewGameProcedure(); break;
                default:
                    return;
            }

            ingameProcedures.Add(handler);
        }
        public static void RemoveIngameHandler(IngameProcedureType type)
        {
            for (int i = ingameProcedures.Count - 1; i >= 0; --i)
            {
                if (type == ingameProcedures[i].HandlerType)
                {
                    ingameProcedures[i].Dispose();
                    ingameProcedures.RemoveAt(i);
                    return;
                }
            }
        }
        public static void MoveNextIngameHandler(IngameProcedureType nextHandlerType)
        {
            LoadingProcedure loadingHandler = new LoadingProcedure(nextHandlerType);
            ingameProcedures.Add(loadingHandler);
        }

        // camera
        public static void InitFollowingCamera(IngameUnitBase player_character)
        {
            ingameCam.InitFollowingCamera(player_character);
        }


        private void Awake()
        {
            // init instance
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // init data table
            AssetManager.Initialize(this.transform);

            // init ingame updaters
            IngameUpdater updater = transform.GetComponent<IngameUpdater>();
            updater.Initialize();

            // init ingame handlers
            ingameProcedures = new List<IngameProcedureBase>();
            
            // init ingame managers
            inputMgr = new InputManager();
            ingameCam = transform.GetComponentInChildren<IngameCamera>(true);
        }
        private void Start()
        {
            AddIngameHander(IngameProcedureType.OPENING);
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