namespace Kompile.Unit.Component
{
    using UnityEngine;
    using Unity.Mathematics;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;

    /// <summary>
    /// [Framework] Component 계층
    /// 무거운 이동 로직을 Manager로 위임하고, 이동 의도(Intent) 데이터만 보관합니다.
    /// </summary>
    public class UnitMoveComponent : MonoBehaviour
    {
        private UnitEntityBase _ownerEntity;

        public float2 CurrentInput { get; private set; }

        public void Initialize(UnitEntityBase owner)
        {
            _ownerEntity = owner;
        }

        public void UpdateIntent(in UnitIntent intent)
        {
            if (!_ownerEntity) return;
            CurrentInput = intent.MoveInput;
        }
    }
}