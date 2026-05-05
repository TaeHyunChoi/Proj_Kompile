namespace Kompile.Unit.Entity
{
    using UnityEngine;
    using Kompile.Unit.Data;
    using static Kompile.Input.Data.Definition;

    /// <summary> 플레이어 입력을 읽어 UnitIntent로 반환하는 Brain </summary>
    public class PlayerControlBrain : IUnitBrain
    {
        public void Initialize(UnitEntityBase entity) { }

        public UnitIntent Update(in InputState inputState)
        {
            float x = 0f, z = 0f;
            if (inputState.IsPressing(IDxInput.RIGHT)) x += 1f;
            if (inputState.IsPressing(IDxInput.LEFT))  x -= 1f;
            if (inputState.IsPressing(IDxInput.UP))    z += 1f;
            if (inputState.IsPressing(IDxInput.DOWN))  z -= 1f;

            return new UnitIntent
            {
                MoveInput   = new Vector2(x, z),
                AnimCommand = UnitAnimCmd.None,
            };
        }

        public void Clear() { }
    }
}
