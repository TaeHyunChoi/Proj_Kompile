namespace Script.GamePlay
{
    using UnityEngine;

    public abstract class ManagerBase : IInputReceiver, IGameUpdater
    {
        public abstract Awaitable Intialize();
        public abstract bool OnInputReceive(Data.DataType.InputState inputState);
        public abstract bool OnUpdate();
        public abstract void Dispose();
    }
}