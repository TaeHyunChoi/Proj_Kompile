using UnityEngine;

namespace Script.GamePlay
{
    public interface IInputReceiver
    {
        bool OnInputReceive(Data.DataType.IDxInput current, Data.DataType.IDxInput prev);
    }
}