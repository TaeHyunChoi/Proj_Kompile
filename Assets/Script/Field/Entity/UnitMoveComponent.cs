namespace Script.Field.Entity
{
    using UnityEngine;

    /// <summary>
    /// [Framework] Component 계층
    /// GameObject에 부착되어 실제 이동 연산(Transform 조작이나 NavMesh 제어 등)을 수행하는 최소 단위.
    /// </summary>
    public class UnitMoveComponent : MonoBehaviour
    {
        private FieldUnitEntity _ownerEntity;
        private Vector3 _targetPosition;
        private bool _isMoving = false;

        public void Initialize(FieldUnitEntity owner)
        {
            _ownerEntity = owner;
        }

        public void SetDestination(Vector3 destination)
        {
            _targetPosition = destination;
            _isMoving = true;
        }

        public void ManualUpdate()
        {
            if (!_isMoving) return;

            // 이동 연산 (예: Vector3.MoveTowards 등)
            // 목적지에 도착하면 _isMoving = false 처리
        }
    }
}