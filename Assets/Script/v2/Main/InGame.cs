using UnityEngine;
using System.Collections.Generic;

namespace Kompile
{
    using Manager;
    
    public class InGame : MonoBehaviour
    {
        private static InGame _instance;

        private List<GameLogicMgrBase> _mgr;
        private CharacterMgr _character;

        private void Awake()
        {
            if (_instance)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _mgr = new List<GameLogicMgrBase>(16);

            _character = new CharacterMgr();

        }
        private void Update()
        {
            InputRouteSystem.OnUpdate();

            _character.OnUpdate();
        }

        public static void AddMgr(GameLogicMgrBase mgr)
        {
            _instance._mgr.Add(mgr);
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
