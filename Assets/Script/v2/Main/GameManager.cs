using UnityEngine;

namespace Kompile
{
    using Kompile.Data;
    using Manager;
    using UnityEditor.AddressableAssets.Build;

    public class GameManager : MonoBehaviour
    {
        private InputRouterManager _input;

        // input_manager 만들어서 input.OnUpdate(); 하면 되는데;
        private void Awake()
        {
            _input = new InputRouterManager();

            // for test;
            //_input.Push(new Temp());
        }
        private void Update()
        {
            _input.OnUpdate();
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
