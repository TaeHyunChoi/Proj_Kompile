namespace Kompile.Entities
{
    using UnityEngine;
    using Data;
    using Domain;

    public class ActorEntity : EntityBase
    {
        private FieldUnitTableData _rawData;
        private ActorFieldMoveComponent _moveCtrl;
        private ActorAnimComponent _animCtrl;
        
        private IUnitBrain _brain;

        public ActorBrainType BrainType => _rawData.BrainType;

        
        public void Initialize(FieldUnitTableData data, FieldActorAnimClip clip, AnimatorOverrideController baseAOC, MapProvider mapProvider)
        {
            _rawData = data;
            ActorBrainType brainType = data.BrainType;
            switch (brainType)
            {
                case ActorBrainType.Player:
                    _brain = new BrainFieldPlayer(this);
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
            ActorIntent intent = _brain.Calculate();

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