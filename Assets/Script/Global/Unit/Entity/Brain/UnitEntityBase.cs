using Script.Global.Unit.Data;

namespace Script.Global.Unit.Entity
{
    using UnityEngine;

    public abstract class UnitEntityBase : MonoBehaviour
    {
        public long InstanceID { get; protected set; }
        public string AssetAddress { get; protected set; }
        public bool IsInitalized { get; protected set; }

        public UnitRuntimeContext Context { get; protected set; }
        protected IUnitBrain _brain;
        
        public void SetAssetAddress(string address)
        {
            AssetAddress = address;
        }
        public void SetBrain(IUnitBrain newBrain)
        {
            _brain?.Clear();

            if (null != newBrain)
            {
                _brain = newBrain;
                _brain.Initialize(this);
            }
        }

        public void Clear()
        {
            IsInitalized = false;
            InstanceID = 0;
            Context = default(UnitRuntimeContext);

            _brain?.Clear();
            _brain = null;
        }

        public abstract void Initialize(long instanceID, UnitRuntimeContext context);
        public abstract void ManualUpdate();
    }    
}