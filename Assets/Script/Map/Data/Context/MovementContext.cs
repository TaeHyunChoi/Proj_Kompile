namespace Script.Map.Data
{
    using Unity.Mathematics;
    using System.Collections.Generic;
    
    public class MovementContext
    {
        public float3[] Path;           // A*로부터 받은 평탄화된 경로
        public int CurrentPathIndex;    // 현재 목표 노드 인덱스
        public float3 CurrentVelocity;  // 현재 이동 속도 (관성 포함)
        
        public float MaxSpeed = 5f;     // 최대 속도
        public float SteeringForce = 10f; // 조향 힘 (높을수록 기민함)
        public float StoppingDistance = 0.5f; // 감속 시작 거리
    }
}