namespace Kompile.Manager
{
    using UnityEngine;
    using System.Collections.Generic;
    
    public class InGame : MonoBehaviour
    {
        private static InGame _instance;
        private GameLogicMgrBase[] _mgr;

        public static Transform Transform => _instance.transform;
        
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
            // InSystemCamera.OnAwake();

            // InGame
            _ = AwakeIngameAsync();
        }
        private async Awaitable AwakeIngameAsync()
        {
            _mgr = new GameLogicMgrBase[]
            {
                new CharacterMgr(),
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

                InDev.Log($"{_mgr[i].GetType()}.OnAwake();");
            }
            
            enabled = true;
        }

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
            for (int i = _mgr.Length - 1; i >= 0; --i)
            {
                _mgr[i].OnDisable();
            }
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
