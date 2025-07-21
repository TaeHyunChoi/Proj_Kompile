namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Data;
    using Script.IngameMessage;

    public partial class IngameManager : MonoBehaviour
    {
        private static IngameManager    instance;

        private static PlayData         playData;

        private static InputManager     inputMgr;
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
        private static Queue<IngameEventType> nextEventTypeQueue;
        public static void EnqueueNextEventType(IngameEventType next)
        {
            nextEventTypeQueue.Enqueue(next);
        }
        public static void MoveNextEventType()
        {
            if (0 >= nextEventTypeQueue.Count)
            {
                return;
            }

            IngameEventType nextEventType = nextEventTypeQueue.Dequeue();
            MessageManager.Publish(new OnMoveNextEvent(nextEventType));
        }

        public static void AddIngameProcedure(IngameProcedureType type)
        {
            InputManager.Clear();

            IngameProcedureBase proc;
            switch (type)
            {
                case IngameProcedureType.OPENING:   proc = new OpeningProcedure();  break;
                case IngameProcedureType.NEW_GAME:  proc = new NewGameProcedure();  break;
                case IngameProcedureType.FIELD:     proc = new x_FieldManager();    break;
                default:
                    return;
            }

            proc.Start();
            ingameProcedures.Add(proc);
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
            inputMgr = new InputManager();
            ingameCam = transform.GetComponentInChildren<IngameCamera>(true);

            // init procedures
            ingameProcedures = new List<IngameProcedureBase>();
            nextEventTypeQueue = new Queue<IngameEventType>();
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