namespace Script.Manager
{
    using Script.Index;
    using Script.Interface;
    using System.Collections.Generic;
    using UnityEngine;

    public class IngameUpdater : MonoBehaviour
    {
        private static List<IIngameUpdater> updateList;
        private static List<IIngameFixedUpdater> fixedUpdateList;
        private static List<IIngameLateUpdater> lateUpdateList;

        public void Initialize()
        {
            updateList = new List<IIngameUpdater>();
            fixedUpdateList = new List<IIngameFixedUpdater>();
            lateUpdateList = new List<IIngameLateUpdater>();
        }

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

        private void Update()
        {
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
    }
}