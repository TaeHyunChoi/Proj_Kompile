namespace Kompile.Field.Entity
{
    using Kompile.Asset.Data;
    using Kompile.Asset.Provider;
    using Kompile.Field.Data;
    using Kompile.Unit.Component;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;
    using UnityEngine;
    
    [RequireComponent(typeof(UnitMoveComponent), typeof(UnitAnimComponent))]
    public class FieldEntity : UnitEntityBase
    {
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;

        public void Initialize(FieldUnitTableData data, FieldUnitAnimClipContext clip, AnimatorOverrideController baseAOC, FieldMapQueryService mapQuery)
        {
            // brain
            switch (data.BrainType)
            {
                case UnitBrainType.Player: _brain = new PlayerControlBrain(this); break;
                default:
                    return;
            }

            // anim-component
            _animComponent = transform.GetComponent<UnitAnimComponent>();
            _animComponent.Initialize(baseAOC, in clip);  

            // move-component
            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            _moveComponent.Initialize(this, mapQuery);
        }

        /// <summary> _brain을 사용하여 owner가 intent를 직접 판단 </summary>
        public void UpdateIntent()
        {
            UnitIntent intent = _brain.Update();
            _moveComponent.UpdateIntent(in intent);
            _animComponent.UpdateIntent(in intent);
        }

        /// <summary> _brain을 사용하지 않고 직접 intent를 주입하는 경우 </summary>
        public void UpdateIntent(in UnitIntent intent)
        {
            //_brain.Update(in intent);
            _moveComponent.UpdateIntent(in intent);
            _animComponent.UpdateIntent(in intent);
        }
    }
}
