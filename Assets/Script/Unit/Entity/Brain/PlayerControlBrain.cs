namespace Kompile.Unit.Entity
{
    using UnityEngine;
    using Kompile.Field.Entity;
    using Script.Input.Provider;
    using static Script.Input.Data.Definition;
    
    /// <summary>
    /// 플레이어 입력을 읽어 FieldPlayerEntity에 이동 벡터를 전달하는 Brain입니다.
    /// IngameInputProvider를 내부에서 생성하고, ManualUpdate 끝에 OnEndOfFrame()을 호출합니다.
    /// </summary>
    public class PlayerControlBrain : IUnitBrain
    {
        private FieldPlayerEntity _playerEntity;
        private IngameInputProvider _input;

        public void Initialize(UnitEntityBase entity)
        {
            _playerEntity = entity as FieldPlayerEntity;
            _input = new IngameInputProvider();
        }

        public void ManualUpdate()
        {
            if (_playerEntity == null || _input == null) return;

            InputState state = _input.Current;

            float x = 0f;
            float z = 0f;

            if (state.IsPressing(IDxInput.RIGHT)) x += 1f;
            if (state.IsPressing(IDxInput.LEFT))  x -= 1f;
            if (state.IsPressing(IDxInput.UP))    z += 1f;
            if (state.IsPressing(IDxInput.DOWN))  z -= 1f;

            // FieldPlayerEntity → UnitMoveComponent.SetMoveInput
            _playerEntity.SetMoveInput(new Vector2(x, z));

            // 프레임 끝 동기화 (latchedInputFlag → prevInputFlag 교체)
            _input.OnEndOfFrame();
        }

        public void Clear()
        {
            _playerEntity = null;
            _input = null;
        }
    }
}
