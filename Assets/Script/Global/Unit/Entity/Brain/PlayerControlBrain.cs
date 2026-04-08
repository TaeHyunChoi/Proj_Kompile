using Script.Field.Entity;

namespace Script.Global.Unit.Entity
{
    public class PlayerControlBrain : IUnitBrain
    {
        private UnitEntityBase _owner;
        
        public void Initialize(UnitEntityBase entity)
        {
            _owner = entity;
        }

        public void ManualUpdate()
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }
    }

}