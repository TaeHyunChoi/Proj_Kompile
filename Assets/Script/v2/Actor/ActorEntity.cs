namespace Kompile.Entity
{
    using UnityEngine;
    using Data;
    using Provider;


    public class ActorEntity : Entity
    {
        private ActorFieldMoveComponent _moveCtrl;
        private ActorAnimComponent _animCtrl;
        private IUnitBrain _brain;

        private UnitBrainType _brainType;
        public UnitBrainType BrainType => _brainType;

        public void Initialize(FieldUnitTableData data, FieldUnitAnimClipContext clip, AnimatorOverrideController baseAOC, MapProvider mapProvider)
        {
            UnitBrainType brainType = data.BrainType;
            switch (brainType)
            {
                case UnitBrainType.Player:
                    _brain = new PlayerControlBrain(this);
                    break;
                default:
                    return;
            }

            _brainType = brainType;

            _moveCtrl = new ActorFieldMoveComponent();
            _moveCtrl.Initialize(transform, mapProvider);

            _animCtrl = transform.GetComponent<ActorAnimComponent>();
            _animCtrl.Initialize(baseAOC, in clip);

            // 
            // ...
        }

        public bool OnUpdate(float deltaTime)
        {
            UnitIntent intent = _brain.Calculate();

            _moveCtrl.OnUpdate(intent.MoveInput, deltaTime);
            _animCtrl.OnUpdate(intent);

            return true;
        }

        public override void Clear()
        {
            base.Clear();
            
            _brain?.Clear();
            _brain = null;

            _moveCtrl.Clear();
            _moveCtrl = null;

            _animCtrl.Clear();
            _animCtrl = null;
        }
    }
}