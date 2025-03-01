namespace Script.Manager
{
    using System.Collections.Generic;
    using UnityEngine;
    using Script.Index;
    using Script.Content;
    using Script.Interface;
    using System;
    using Script.Data;

    public class IngameManager : MonoBehaviour
    {
        private static IngameManager    instance;
        private static InputManager     inputMgr;
        private static FieldMapManager  fieldMapMgr;

        private static List<IngameLogicBase>      ingameLogics;
        private static List<IIngameUpdater>       update;
        private static List<IIngameFixedUpdater>  fixedUpdate;
        private static List<IIngameLateUpdater>   lateUpdate;
        private readonly int clearSuccessIngameCount = 5;

        public static void AddInput(AssetCode assetType, IIngameInput targetInput)
        {
            inputMgr.Add(assetType, targetInput);
        }
        public static void RemoveInput(AssetCode assetType)
        {
            inputMgr.Remove(assetType);
        }


        public static void AddIngame(IngameLogicBase targetIngame)
        {
            ingameLogics.Add(targetIngame);
        }
        public static void AddUpdater(IIngameUpdater targetUpdater)
        {
            for (int i = 0; i < update.Count; ++i)
            {
                if (null == update[i])
                {
                    update[i] = targetUpdater;
                    return;
                }
            }

            update.Add(targetUpdater);
        }
        public static void AddFixedUpdater(IIngameFixedUpdater targetFixedUpdater)
        {
            for (int i = 0; i < fixedUpdate.Count; ++i)
            {
                if (null == fixedUpdate[i])
                {
                    fixedUpdate[i] = targetFixedUpdater;
                    return;
                }
            }

            fixedUpdate.Add(targetFixedUpdater);
        }
        public static void AddLateUpdater(IIngameLateUpdater targetLateUpdater)
        {
            for (int i = 0; i < lateUpdate.Count; ++i)
            {
                if (null == lateUpdate[i])
                {
                    lateUpdate[i] = targetLateUpdater;
                    return;
                }
            }

            lateUpdate.Add(targetLateUpdater);
        }

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

            AssetManager.Initialize(this.transform);
            
            inputMgr    = new InputManager();
            fieldMapMgr = new FieldMapManager();
            
            ingameLogics = new List<IngameLogicBase>();

            // init: update
            update      = new List<IIngameUpdater>();
            fixedUpdate = new List<IIngameFixedUpdater>();
            lateUpdate  = new List<IIngameLateUpdater>();
        }

        private void Start()
        {
            AddIngame(new Ingame_Opening());
        }

        private void Update()
        {
            // 예외 : 입력은 최우선으로 업데이트
            if (true == inputMgr.IsPerformed())
            {
                inputMgr.Update();
            }

            // update: contents
            int nullCount = 0;
            for (int i = 0; i < update.Count; ++i)
            {
                if (null == update[i])
                {
                    ++nullCount;
                    continue;
                }

                UpdaterState state = update[i].UpdateState();

                switch (state)
                {
                    case UpdaterState.SUCCESS:
                        update[i] = null;

                        // 되려나..?
                        if (++nullCount > clearSuccessIngameCount)
                        {
                            List<IIngameUpdater> newUpdater = new List<IIngameUpdater>();
                            for (int u = 0; u < update.Count; ++u)
                            {
                                if (null != update[u])
                                {
                                    newUpdater.Add(update[u]);
                                }
                            }
                            update = newUpdater;
                            GC.Collect();
                        }
                        break;
                    case UpdaterState.FAILURE:
#if TEST_BUILD
                        DevError.DebugAssert(ErrorCode.FAIL_TASK, updates[i].Type.ToString());
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
            for (int i = 0; i < fixedUpdate.Count; ++i)
            {
                if (null == fixedUpdate[i])
                {
                    continue;
                }

                UpdaterState state = fixedUpdate[i].FixedUpdateState();
                switch (state)
                {
                    case UpdaterState.SUCCESS:
                        // .Publish(END_UPDATE)
                        fixedUpdate[i] = null;
                        break;
                    case UpdaterState.FAILURE:
#if TEST_BUILD
                        DevError.DebugAssert(ErrorCode.FAIL_TASK, updates[i].Type.ToString());
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
            int emptyCount = 0;

            for (int i = 0; i < lateUpdate.Count; ++i)
            {
                if (null == lateUpdate[i])
                {
                    ++emptyCount;
                    continue;
                }

                UpdaterState state = lateUpdate[i].LateUpdateState();
                switch (state)
                {
                    case UpdaterState.SUCCESS:
                        lateUpdate[i] = null;
                        ++emptyCount;
                        break;
                    case UpdaterState.FAILURE:
#if TEST_BUILD
                        DevError.DebugAssert(ErrorCode.FAIL_TASK, updates[i].Type.ToString());
#endif
                        break;
                    default:
                        // Running
                        break;
                }
            }

            if (emptyCount > clearSuccessIngameCount)
            {
                //재정렬?
            }
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

