namespace Script.GamePlay
{
    using UnityEngine;

    public abstract class ManagerBase : IInputReceiver, IGameUpdater
    {
        public abstract Awaitable Intialize();
        public abstract bool OnInputReceive(Data.DataType.IDxInput current, Data.DataType.IDxInput prev);
        public abstract bool OnUpdate();
        public abstract void Dispose();
    }
}