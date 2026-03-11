namespace Script.Map.Utility
{
    using Unity.Burst;
    using Unity.Mathematics;

    [BurstCompile]
    public class MapNaviSteeringUtil
    {
        // 조향 힘 계산: (원하는 방향 * 속도) - 현재 속도
        public static float3 CalculateSteering(float3 currentPos, float3 targetPos, float3 currentVelocity, float maxSpeed, float force)
        {
            float3 desiredDirection = targetPos - currentPos;
            float distance = math.length(desiredDirection);

            if (distance < 0.01f) return float3.zero;

            // 목표 방향으로의 최대 속도 벡터
            float3 desiredVelocity = math.normalize(desiredDirection) * maxSpeed;

            // 조향 힘 도출
            float3 steering = desiredVelocity - currentVelocity;
            return steering * force;
        }

        // 2.5D 스프라이트 방향 결정 (8방향 인덱스 반환: 0~7)
        public static int GetSpriteDirection8(float3 velocity)
        {
            if (math.lengthsq(velocity) < 0.001f) return -1; // 정지 상태

            // XZ 평면상의 각도 계산
            float angle = math.atan2(velocity.x, velocity.z) * UnityEngine.Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // 45도 단위로 8방향 매핑
            return (int)math.round(angle / 45f) % 8;
        }
    }
}