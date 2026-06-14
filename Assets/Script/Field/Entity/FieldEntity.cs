namespace Kompile.Field.Entity
{
    using Kompile.Asset.Data;
    using Kompile.Asset.Provider;
    using Kompile.Field.Data;
    using Kompile.Unit.Component;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;
    using UnityEngine;

    [RequireComponent(typeof(UnitMoveComponent), typeof(UnitAnimComponent))]
    public class FieldEntity : UnitEntityBase
    {
        private UnitMoveComponent _moveComponent;
        private UnitAnimComponent _animComponent;
        
        public UnitMoveComponent MoveComponent => _moveComponent;

        public void Initialize(FieldUnitTableData data, FieldUnitAnimClipContext clip, AnimatorOverrideController baseAOC, FieldMapQueryService mapQuery = null)
        {
            // 💡 1. Brain 초기화: 각 Brain의 기존 스펙을 철저히 존중합니다.
            switch (data.BrainType)
            {
                case UnitBrainType.Player:
                    // 플레이어 브레인은 원래 나으리의 코드대로 this만 전달하여 컴파일 에러를 해결합니다.
                    _brain = new PlayerControlBrain(this);
                    break;

                // 💡 훗날 지형을 스스로 판단해야 하는 NPC나 몬스터 AI가 추가될 때 예시
                // case UnitBrainType.MonsterAI:
                //     // 맵을 읽고 길을 찾아야 하는 AI 브레인에게만 선택적으로 mapQuery를 주입합니다.
                //     _brain = new MonsterAttackBrain(this, mapQuery); 
                //     break;

                default:
                    return;
            }

            // 2. Anim Component 초기화
            _animComponent = transform.GetComponent<UnitAnimComponent>();
            _animComponent.Initialize(baseAOC, in clip);

            // 3. Move Component 초기화: 맵 정보가 필요 없는 순수 데이터 껍데기 구조 유지
            _moveComponent = transform.GetComponent<UnitMoveComponent>();
            _moveComponent.Initialize(this);
        }

        /// <summary> _brain을 사용하여 owner가 intent를 직접 판단 (AI 유닛용) </summary>
        public void UpdateIntent()
        {
            if (_brain == null) return;
            UnitIntent intent = _brain.Update();
            _moveComponent.UpdateIntent(in intent);
            _animComponent.UpdateIntent(in intent);
        }

        /// <summary> _brain을 사용하지 않고 외부(Manager)에서 직접 intent를 주입하는 경우 (현재 플레이어용) </summary>
        public void UpdateIntent(in UnitIntent intent)
        {
            _moveComponent.UpdateIntent(in intent);
            _animComponent.UpdateIntent(in intent);
        }
    }
}