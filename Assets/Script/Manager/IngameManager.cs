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

        private static FieldManager fieldMgr;

        private static PlayData         playData;

        private static InputHandler     inputHandler;
        private static IngameCamera     ingameCam;

        private static List<IngameProcedureBase> ingameProcedures;


        private static Transform[] canvasTransforms;
        private static Transform maptRootTransform;
        private static Transform unitRootTransform;

        public static Transform UnitRootTransform => unitRootTransform;
        public static Transform MapRootTransform => maptRootTransform;
        public static Transform UICameraRootTransform => canvasTransforms[(int)CanvasType.CAMERA];
        public static Transform UIOverayRootTransform => canvasTransforms[(int)CanvasType.OVERLAY];


        private static ContentStateMachine contentStateMachine;


        // manager
        public static async void EnterField(PlayData data)
        {
#if UNITY_EDITOR
            Debug.Log("[IngameManager] EnterNewField");
#endif

            playData = data;

            fieldMgr = new FieldManager();
            await fieldMgr.Initialize(playData);
        }

        // ingame procedure
        //public static void AddIngameProcedure(IngameProcedureType type)
        //{
        //    InputHandler.Clear();

        //    IngameProcedureBase proc;
        //    switch (type)
        //    {
        //        case IngameProcedureType.OPENING:   proc = new OpeningProcedure();      break;
        //        case IngameProcedureType.NEW_GAME:  proc = new NewGameProcedure();      break;
        //        default:
        //            return;
        //    }

        //    ingameProcedures.Add(proc);
        //    proc.Start();
        //}
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

            contentStateMachine = new ContentStateMachine();

            // get asset
            AssetManagerV2.Initialize();

            canvasTransforms = new Transform[3];
            Transform uiParent = transform.Find("UI");
            for (int i = 0; i < canvasTransforms.Length; ++i)
            {
                canvasTransforms[i] = uiParent.GetChild(i);
            }

            //maptRootTransform = transform.Find("Map").transform;
            //unitRootTransform = transform.Find("Unit").transform;

            //// init updater
            //IngameUpdater updater = transform.GetComponent<IngameUpdater>();
            //updater.Initialize();

            //// init manager
            //inputHandler = new InputHandler();
            //ingameCam = transform.GetComponentInChildren<IngameCamera>(true);

            //// init procedures
            //ingameProcedures = new List<IngameProcedureBase>();
        }

        private void Start()
        {
            contentStateMachine.ChangeState(new OpeningContent());

            //AddIngameProcedure(IngameProcedureType.OPENING);
        }
        private void OnEnable()
        {
            //inputHandler.OnEnable();
        }
        private void OnDisable()
        {
            //inputHandler.OnDisable();

            //if (ingameProcedures != null)
            //{
            //    for (int i = ingameProcedures.Count - 1; i >= 0; --i)
            //    {
            //        ingameProcedures[i].Dispose();
            //    }
            //}

            //fieldMgr.Release();
        }


        public static void ChangeState(IContentState newState)
        {
            contentStateMachine.ChangeState(newState);
        }

    }
}