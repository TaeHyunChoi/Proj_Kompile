namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Interface;
    using Script.Data;
    using static Script.Util.FuncUtil;

    public class IngameManager : MonoBehaviour
    {
        private const int RESET_UPDATER_LIST_COUNT = 10;

        private static IngameManager    instance;
        private static InputManager     inputMgr;
        private static FieldMapManager  fieldMapMgr;

        // ingame handler끼리 서로 데이터를 주고 받을 수도 있으므로 ingame manager에서 들고 있자.
        // 가장 마지막에 추가한 ingame_handler에게 input.Update()할 것이다.
        private static List<_IngameHandlerBase> ingameHandler;
        private static int targetHandlerIndex;

        private static List<IIngameUpdater>[]   updaterList;
        private static List<IIngameUpdater> Updater      { get => updaterList[0]; set => updaterList[0] = value; }
        private static List<IIngameUpdater> FixedUpdater { get => updaterList[1]; set => updaterList[1] = value; }
        private static List<IIngameUpdater> LateUpdater  { get => updaterList[2]; set => updaterList[2] = value; }

        // manage_ingame_handler
        public static void AddIngameHander(IngameHandlerType type)
        {
            _IngameHandlerBase handler;
            switch (type)
            {
                case IngameHandlerType.OPENING:
                    handler = new OpeningHandler();
                    break;
                case IngameHandlerType.NEW_GAME:
                    handler = new NewGameHandler();
                    break;
                default:

                    return;
            }

            ingameHandler.Add(handler);
            handler.MoveNext();
            targetHandlerIndex += 1;
        }
        public static void MoveNextHandler(IngameHandlerType type)
        {
            if (false == ingameHandler.TryGetIngameHandler(type, out var handler))
            {
                return;
            }

            if (IngameHandlerState.SUCCESS == handler.MoveNext())
            {
                handler.Dispose();
                ingameHandler.Remove(handler);
                targetHandlerIndex -= 1;
            }
        }

        // manage_updater
        public static void AddUpdater(UpdaterType type, IIngameUpdater addUpdater)
        {
            int index = (int)type;

            for (int i = 0; i < updaterList[index].Count; ++i)
            {
                if (null == updaterList[index][i])
                {
                    updaterList[index][i] = addUpdater;
                    return;
                }
            }

            updaterList[index].Add(addUpdater);
        }
        public static void RemoveInputUpdater(IIngameUpdater updater)
        {
            for (int i = 0; i < Updater.Count; ++i)
            {
                if (updater == Updater[i])
                {
                    Updater[i] = null;
                }
            }
            //Updater.Remove(updater);
        }

        // 얘는 FieldExploreHandler로 빠질 수도?
        public static bool TryAddMapRawGridData(int gridKey, MapGridData rawMapGridData)
        {
            return fieldMapMgr.TryAddMapGridData(gridKey, rawMapGridData);
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

            updaterList = new List<IIngameUpdater>[3] { new List<IIngameUpdater>(), new List<IIngameUpdater>(), new List<IIngameUpdater>() };

            AssetManager.Initialize(this.transform);

            inputMgr      = new InputManager();
            fieldMapMgr   = new FieldMapManager();

            ingameHandler = new List<_IngameHandlerBase>();
            targetHandlerIndex = -1;
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

            // update: contents
            int nullCount = 0;
            for (int i = 0; i < Updater.Count; ++i)
            {
                if (null == Updater[i])
                {
                    ++nullCount;
                    continue;
                }

                switch (Updater[i].UpdateState())
                {
                    case IngameUpdateState.SUCCESS:
                        ++nullCount;
                        Updater[i] = null;
                        break;
                    case IngameUpdateState.FAILURE:
#if TEST_BUILD
                        DevError.DebugAssert(ErrorCode.FAIL_TASK, updates[i].Type.ToString());
#endif
                        break;
                    default:
                        // Running
                        break;
                }
            }

            // 쪼오금 애매하지만 일단 남기기로.
            if (nullCount > RESET_UPDATER_LIST_COUNT)
            {
                List<IIngameUpdater> newUpdater = new List<IIngameUpdater>();
                for (int u = 0; u < Updater.Count; ++u)
                {
                    if (null != Updater[u])
                    {
                        newUpdater.Add(Updater[u]);
                    }
                }

                Updater = newUpdater;
            }
        }
        //private void FixedUpdate() { }
        //private void LateUpdate()  { }

        public static void GetInput(IDxInput.InputFlag inputFlag)
        {
            if (targetHandlerIndex < 0)
            {
                return;
            }

            ingameHandler[targetHandlerIndex].ReceiveInput(inputFlag);
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