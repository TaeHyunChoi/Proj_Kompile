namespace Kompile.Entity
{
    using Data;
    using Manager;
    using UnityEngine;
    
    /// <summary> 플레이어 입력을 읽어 UnitIntent로 반환하는 Brain </summary>
    public class FieldPlayerBrainContext : IUnitBrain
    {
        private Entity _owner;


        public FieldPlayerBrainContext(Entity entity)
        {
            _owner = entity;
        }
        public void Clear()
        {
            _owner = null;
        }


        public ActorIntent Calculate()
        {
            ActorIntent intent;
            InputState input = InGame.Input.Current;

            if (input.IsDown(IDxInput.CANCEL | IDxInput.ENTER | IDxInput.ACTION))
            {
                intent = InGame.Field.GetPlayerInteractionIntent(_owner, input);
            }
            else if (input.TryGetDirection(out Vector2 dir))
            {
                intent = new ActorIntent(dir, EntityState.Walk);
            }
            else
            {
                intent = new ActorIntent(dir, EntityState.Idle);
            }

            return intent;
        }
    }
}
