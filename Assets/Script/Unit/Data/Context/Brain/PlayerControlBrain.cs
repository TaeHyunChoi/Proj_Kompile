namespace Kompile.Unit.Entity
{
    using UnityEngine;
    using Kompile.Unit.Data;
    using Kompile.Input.Provider;
    using static Kompile.Input.Data.Definition;

    /// <summary> 플레이어 입력을 읽어 UnitIntent로 반환하는 Brain </summary>
    public class PlayerControlBrain : IUnitBrain
    {
        private IngameInputProvider _input;

        public void Initialize(UnitEntityBase entity)
        {
            _input = new IngameInputProvider();
        }

        public UnitIntent Update()
        {
            if (_input is null)
            {
                return UnitIntent.Empty;
            }

            InputState state = _input.Current;

            float x = 0f, z = 0f;
            if (state.IsPressing(IDxInput.RIGHT)) x += 1f;
            if (state.IsPressing(IDxInput.LEFT))  x -= 1f;
            if (state.IsPressing(IDxInput.UP))    z += 1f;
            if (state.IsPressing(IDxInput.DOWN))  z -= 1f;

            // 프레임 끝 동기화 (latchedInputFlag → prevInputFlag 교체)
            _input.OnEndOfFrame();

            return new UnitIntent
            {
                MoveInput   = new Vector2(x, z),
                AnimCommand = UnitAnimCmd.None,
            };
        }

        public void Clear()
        {
            _input = null;
        }
    }
}
