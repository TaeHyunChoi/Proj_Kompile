namespace Kompile.Entity
{
    using Data;
    using UnityEngine;
    
    public class ActorEntity : Entity
    {
        private UnitAnimComponent _animCtrl;
        private IUnitBrain _brain;
        
        public void Initialize(FieldUnitTableData data, FieldUnitAnimClipContext clip, AnimatorOverrideController baseAOC)
        {
            switch (data.BrainType)
            {
                case UnitBrainType.Player:
                    _brain = new PlayerControlBrain(this);
                    break;
            }
            
            _animCtrl = transform.GetComponent<UnitAnimComponent>();
            _animCtrl.Initialize(baseAOC, in clip);
            
            // for test
            _animCtrl.UpdateIntent(new UnitIntent(Vector3.forward));
        }

        public override void Clear()
        {
            base.Clear();
            _brain?.Clear();
            _brain = null;
        }
    }
}
