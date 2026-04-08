namespace Script.Field.Entity
{
    using Script.Global.Unit.Entity;
    using Script.Global.Unit.Data;
    using UnityEngine;

    [RequireComponent(typeof(UnitMoveComponent), typeof(UnitAnimComponent))]
    public class FieldPlayerEntity : UnitEntityBase
    {   
        // player in field
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;


        public override void Initialize(long instanceID, UnitRuntimeContext context)
        {
            InstanceID = instanceID;
            Context = context;

            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            _animComponent = transform.GetComponent<UnitAnimComponent>();
            
            _moveComponent.Initialize(this);
            _animComponent.Initialize(this);

            IsInitalized = true;
        }

        public override void ManualUpdate()
        {
            _brain.ManualUpdate();
            _moveComponent.ManualUpdate();
            _animComponent.ManualUpdate();
        }
    }
}