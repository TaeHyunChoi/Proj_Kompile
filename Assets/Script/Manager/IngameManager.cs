namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Data;

    public partial class IngameManager : MonoBehaviour
    {
        private static IngameManager    instance;

        private static PlayData         playData;

        private static InputHandler     inputHandler;
        private static IngameCamera     ingameCam;

        private static List<IngameProcedureBase> ingameProcedures;

        // play data
        public static void AddNewPlayData()
        {
            playData = new PlayData();

#if UNITY_EDITOR
            Debug.Log("NEWGAME_PLAYER_DATA");
#endif
        }
        public static PlayData GetPlayData()
        {
            return playData;
        }

        // ingame procedure
        public static void AddIngameProcedure(IngameProcedureType type)
        {
            InputHandler.Clear();

            IngameProcedureBase proc;
            switch (type)
            {
                case IngameProcedureType.OPENING:   proc = new OpeningProcedure();  break;
                case IngameProcedureType.NEW_GAME:  proc = new NewGameProcedure();  break;
                case IngameProcedureType.FIELD:     proc = new EnterFieldProcedure();    break;
                default:
                    return;
            }

            ingameProcedures.Add(proc);
            proc.Start();
        }
        public static void RemoveIngameProcedure(IngameProcedureType type)
        {
            for (int i = ingameProcedures.Count - 1; i >= 0; --i)
            {
                if (true == ingameProcedures[i].IsType(type))
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
            inputHandler = new InputHandler();
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
            inputHandler.OnEnable();
        }
        private void OnDisable()
        {
            inputHandler.OnDisable();
        }
    }
}