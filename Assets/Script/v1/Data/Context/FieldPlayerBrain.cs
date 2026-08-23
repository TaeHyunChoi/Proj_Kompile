namespace Kompile.Entity
{
    using Data;
    using Manager;
    using UnityEngine;
    
    /// <summary> 플레이어 입력을 읽어 UnitIntent로 반환하는 Brain </summary>
    public class FieldPlayerBrain : IUnitBrain
    {
        // _owner로부터 데이터를 받아서 _brain에서 처리하는 것도 상정해야;
        private Entity _owner;


        public FieldPlayerBrain(Entity entity)
        {
            _owner = entity;
        }
        public void Clear()
        {
            _owner = null;
        }


        public UnitIntent Calculate()
        {
            UnitIntent intent;
            Definition.InputState input = InGame.Input.Current;

            if (input.IsDown(Definition.IDxInput.CANCEL | Definition.IDxInput.ENTER | Definition.IDxInput.ACTION))
            {
                intent = InGame.Field.GetPlayerInteractionIntent(_owner, input);
            }
            else if (input.TryGetDirection(out Vector2 dir))
            {
                intent = new UnitIntent(dir, EntityState.Walk);
            }
            else
            {
                intent = new UnitIntent(dir, EntityState.Idle);
            }

            return intent;
        }
    }
}
