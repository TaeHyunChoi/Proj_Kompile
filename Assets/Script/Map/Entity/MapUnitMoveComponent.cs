namespace Script.Map.Entity
{
    using UnityEngine;
    using Unity.Mathematics;
    using Script.Map.Data;
    using Script.Map.Utility;
    
    public class MapUnitMoveComponent : MonoBehaviour
    {
        
        private MovementContext _context;

        public void Initialize(float3[] smoothedPath)
        {
            _context = new MovementContext 
            { 
                Path = smoothedPath, 
                CurrentPathIndex = 0,
                CurrentVelocity = float3.zero,
                MaxSpeed = 5f,
                SteeringForce = 10f,
                StoppingDistance = 0.1f // 아주 가까워지면 도착 판정
            };
        }

        private void Update()
        {
            if (_context?.Path == null || _context.CurrentPathIndex >= _context.Path.Length) return;

            float3 currentPos = transform.position;
            float3 targetPos = _context.Path[_context.CurrentPathIndex];

            // 1. Steering 계산 (도착 감속 로직인 Arrival이 포함된다면 여기서 처리)
            float3 steering = MapNaviSteeringUtil.CalculateSteering(
                currentPos, 
                targetPos, 
                _context.CurrentVelocity, 
                _context.MaxSpeed, 
                _context.SteeringForce
            );

            // 2. 가속도(Steering)를 속도(Velocity)에 누적
            _context.CurrentVelocity += steering * Time.deltaTime;
            
            // 최대 속도 클램핑
            if (math.lengthsq(_context.CurrentVelocity) > _context.MaxSpeed * _context.MaxSpeed)
            {
                _context.CurrentVelocity = math.normalize(_context.CurrentVelocity) * _context.MaxSpeed;
            }

            // 3. 순수 수학적 이동 (물리 엔진 없이 Transform만 변경)
            // A* 경로에 이미 높이(Y)가 포함되어 있으므로, 3D 공간을 자연스럽게 날아가듯/걸어가듯 이동함
            float3 nextPos = currentPos + (_context.CurrentVelocity * Time.deltaTime);
            transform.position = nextPos;

            // 4. 노드 도착 판정 (도달 거리는 매우 짧게)
            if (math.distance(currentPos, targetPos) < _context.StoppingDistance)
            {
                _context.CurrentPathIndex++;
                
                // 만약 마지막 노드(최종 목적지)에 도달했다면 속도를 0으로 초기화하여 미끄러짐 방지
                if (_context.CurrentPathIndex >= _context.Path.Length)
                {
                    _context.CurrentVelocity = float3.zero;
                }
            }

            // 5. 2.5D 비주얼 업데이트 (8방향 스프라이트 변경)
            int dirIndex = MapNaviSteeringUtil.GetSpriteDirection8(_context.CurrentVelocity);
            UpdateSprite(dirIndex);
        }

        private void UpdateSprite(int dirIndex)
        {
            if (dirIndex == -1) return;
            // TODO: 방향에 따른 픽셀 아트 애니메이션 변경
        }
    }
}
