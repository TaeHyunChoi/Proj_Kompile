namespace Kompile.Manager
{
    using UnityEngine;

    public partial class InGame : MonoBehaviour
    {
        private static InGame _instance;

        private GameLogicMgrBase[] _mgr;

        public static Transform Transform => _instance.transform;

        // short-cut property for manager access
        public static ActorMgr Actor => Get<ActorMgr>();
        public static FieldMgr Field => Get<FieldMgr>();


        // --- Manager: Property : 필요하면 추가하고, 귀찮으면 .Get<T>()로 참조한다.
        //public static ActorMgr Actor => Get<ActorMgr>();
        //public static FieldMgr Field => Get<FieldMgr>();
        //public static MapMgr Map => Get<MapMgr>();


        // --- MonoBehaviour Loop
        private void Awake()
        {
            if (_instance)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            enabled = false;

            // InSystem
            InSystemCamera inSystemCamera = transform.GetComponentInChildren<InSystemCamera>();
            inSystemCamera.OnAwake();

            // InGame
            _ = AwakeIngameAsync();
        }
#if DEV_BUILD
        private void Start()
        {
            //Get<ActorMgr>().Spawn(); // 대충 요런 느낌으로 구현해야 하네
        }
#endif
        private void Update()
        {
            InSystemInput.OnUpdate();

            for (int i = 0; i < _mgr.Length; ++i)
            {
                _mgr[i].OnUpdate();
            }
        }
        private void OnDisable()
        {
            if (null == _mgr || 0 == _mgr.Length)
            {
                return;
            }

            for (int i = _mgr.Length - 1; i >= 0; --i)
            {
                _mgr[i].OnDisable();
            }
        }


        // --- Manager: Function
        private async Awaitable AwakeIngameAsync()
        {
            _mgr = new GameLogicMgrBase[]
            {
                new ActorMgr(),
                new FieldMgr()
            };

            // prior 순으로 정렬 (bubble sort)
            GameLogicMgrBase swap;
            for (int i = 0; i < _mgr.Length - 1; i++)
            {
                for (int j = 0; j < _mgr.Length - 1 - i; j++)
                {
                    if (_mgr[j].Prior > _mgr[j + 1].Prior)
                    {
                        swap = _mgr[j];
                        _mgr[j] = _mgr[j + 1];
                        _mgr[j + 1] = swap;
                    }
                }
            }

            for (int i = 0; i < _mgr.Length; ++i)
            {
                bool awake = await _mgr[i].OnAwake();
                if (!awake)
                {
                    InDev.LogError($"fail {_mgr[i].GetType()}.OnAwake();");
                    return;
                }

                _mgr[i].RegisterToCache();
                InDev.Log($"{_mgr[i].GetType().Name}.OnAwake();");
            }

            enabled = true;
        }
        public static void Register<T>(T manager) where T : GameLogicMgrBase
        {
            ManagerCache<T>.Instance = manager;
        }
        public static T Get<T>() where T : GameLogicMgrBase
        {
            return ManagerCache<T>.Instance;
        }
    }

    //public class Temp : IInputReceivable
    //{
    //    public bool OnReceiveInput(Definition.IDxInput inputState)
    //    {
    //        Debug.Log($"[DEBUG] {inputState}");
    //        return true;
    //    }
    //}
}
