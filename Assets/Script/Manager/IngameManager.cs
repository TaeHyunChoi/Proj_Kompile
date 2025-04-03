namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Interface;
    using Script.Data;

    public class IngameManager : MonoBehaviour
    {
        private const int RESET_UPDATER_LIST_COUNT = 10;

        private static IngameManager    instance;
        private static InputManager     inputMgr;
        private static FieldMapManager  fieldMapMgr;

        // ingame handler끼리 서로 데이터를 주고 받을 수도 있으므로 ingame manager에서 들고 있자.
        private static Dictionary<IngameHandlerType, _IngameHandlerBase> ingameHandler;

        private static IIngameUpdater           inputUpdater;
        private static List<IIngameUpdater>[]   updaterList;
        private static List<IIngameUpdater> Updater      { get => updaterList[0]; set => updaterList[0] = value; }
        private static List<IIngameUpdater> FixedUpdater { get => updaterList[1]; set => updaterList[1] = value; }
        private static List<IIngameUpdater> LateUpdater  { get => updaterList[2]; set => updaterList[2] = value; }

        public static void AddIngameHandler(_IngameHandlerBase targetIngame)
        {
            ingameHandler.Add(targetIngame.HandlerType, targetIngame);
        }
        public static void AddInputUpdater(IIngameUpdater addUpdater)
        {
            inputUpdater = addUpdater;
        }
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

        public static void RemoveIngameHandler(IngameHandlerType handlerType)
        {
            ingameHandler[handlerType].Dispose();
            ingameHandler.Remove(handlerType);
        }
        public static void RemoveInputUpdater(IIngameUpdater updater)
        {
            Updater.Remove(updater);
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
            ingameHandler = new Dictionary<IngameHandlerType, _IngameHandlerBase>();
        }
        private void Start()
        {
            AddIngameHandler(new OpeningHandler());
        }


        private void Update()
        {
            // update: input
            if (IngameUpdateState.FAILURE == inputUpdater.UpdateState())
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