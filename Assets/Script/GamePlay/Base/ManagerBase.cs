namespace Script.GamePlay
{
    using System;
    using Script.Data;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class ManagerBase : IInputReceiver, IGameUpdater
    {
        public abstract Awaitable Intialize(Dictionary<Type, ISystem> systems);
        public abstract bool OnInputReceive(Data.DataType.IDxInput inputFlag);
        public abstract bool OnUpdate();
    }
}