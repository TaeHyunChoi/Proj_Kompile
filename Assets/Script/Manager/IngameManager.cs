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

        // ingame procedure
        public static void AddIngameProcedure(IngameProcedureType type)
        {
            IngameProcedureBase proc;
            switch (type)
            {
                case IngameProcedureType.OPENING:     proc = new OpeningProcedure(); break;
                case IngameProcedureType.NEW_GAME:    proc = new NewGameProcedure(); break;
                default:
                    return;
            }

            ingameProcedures.Add(proc);
        }
        public static void RemoveIngameProcedure(IngameProcedureType type)
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

            // init updater
            IngameUpdater updater = transform.GetComponent<IngameUpdater>();
            updater.Initialize();

            // init manager
            inputMgr = new InputManager();
            ingameCam = transform.GetComponentInChildren<IngameCamera>(true);

            // init procedures
            ingameProcedures = new List<IngameProcedureBase>();
        }
        private void Start()
        {
            AddIngameProcedure(IngameProcedureType.OPENING);
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