namespace Kompile.Entity
{
    using Data;
    using Manager;

    /// <summary> 플레이어 입력을 읽어 UnitIntent로 반환하는 Brain </summary>
    public class PlayerControlBrain : IUnitBrain
    {
        // _owner로부터 데이터를 받아서 _brain에서 처리하는 것도 상정해야;
        private Entity _owner;


        public PlayerControlBrain(Entity entity)
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

            Definition.InputState Input = InGame.Input.Current;

            // 상호 작용 - 이걸 여기서 처리하는게 맞나?
            // 매니저 불러와! 상태가 되었네;
            if (Input.IsDown(Definition.IDxInput.CANCEL))
            {

            }
            if (Input.IsDown(Definition.IDxInput.ENTER))
            {

            }
            if (Input.IsDown(Definition.IDxInput.ACTION))
            {

            }

            // 이동
            if (Input.TryGetDirection(out var dir))
            {
                intent = new UnitIntent(dir, UnitAnimCmd.Walk);
            }
            else
            {
                intent = new UnitIntent(dir, UnitAnimCmd.Idle);
            }

            return intent;
        }
    }
}
