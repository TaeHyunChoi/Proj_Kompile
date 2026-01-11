using UnityEngine;

namespace Script.GamePlay
{
    public interface IInputReceiver
    {
        bool OnInputReceive(Data.DataType.InputState inputState);
    }
}