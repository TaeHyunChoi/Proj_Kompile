namespace Kompile.Entity
{
    using UnityEngine;
    using Data;
    using Provider;


    public class ActorEntity : Entity
    {
        private FieldUnitTableData _rawData;
        private ActorFieldMoveComponent _moveCtrl;
        private ActorAnimComponent _animCtrl;
        
        private IUnitBrain _brain;

        public UnitBrainType BrainType => _rawData.BrainType;

        
        public void Initialize(FieldUnitTableData data, FieldUnitAnimClipContext clip, AnimatorOverrideController baseAOC, MapProvider mapProvider)
        {
            _rawData = data;
            UnitBrainType brainType = data.BrainType;
            switch (brainType)
            {
                case UnitBrainType.Player:
                    _brain = new FieldPlayerBrain(this);
                    break;
                default:
                    return;
            }

            _moveCtrl = new ActorFieldMoveComponent();
            _moveCtrl.Initialize(transform, mapProvider);

            _animCtrl = transform.GetComponent<ActorAnimComponent>();
            _animCtrl.Initialize(baseAOC, in clip);
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