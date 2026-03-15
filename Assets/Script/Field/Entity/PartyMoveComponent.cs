namespace Script.Field.Entity.Component
{
    using UnityEngine;
    using Unity.Mathematics;

    /// <summary>
    /// [Framework] Component: JRPG 스타일의 파티원 궤적 추적 이동 (환형 버퍼 최적화 버전)
    /// 동적 할당(Queue)을 제거하고 고정 크기 배열을 사용하여 메모리 재할당과 GC 발생을 원천 차단합니다.
    /// </summary>
    public class PartyMoveComponent : MonoBehaviour
    {
        [Header("JRPG Follow Settings")]
        [Tooltip("선행 엔티티와 유지할 목표 거리 간격입니다.")]
        public float FollowDistance = 1.0f;
        
        [Tooltip("선행자의 위치를 궤적으로 기록할 최소 거리 간격입니다.")]
        public float RecordInterval = 0.2f;

        private Transform targetTransform;
        
        // --- 환형 버퍼 (Circular Buffer) 상태 관리 ---
        // 큐(Queue) 대신 사용할 고정 크기의 float3 배열입니다.
        private float3[] pathRingBuffer;
        
        private int headIndex;      // 쓰기 위치 (선행자의 궤적을 기록할 인덱스)
        private int tailIndex;      // 읽기 위치 (파티원이 따라갈 목적지를 꺼낼 인덱스)
        private int bufferCount;    // 현재 버퍼에 담긴 유효한 웨이포인트 개수
        private int bufferCapacity; // 배열의 최대 수용량

        private float3 lastRecordedTargetPos;
        private float3 currentWaypoint;
        private bool hasWaypoint;

        /// <summary>
        /// Manager가 파티원을 생성할 때 호출합니다. 여기서 버퍼 크기를 단 한 번만 계산하여 할당합니다.
        /// </summary>
        public void Initialize(Transform target, float distance)
        {
            targetTransform = target;
            FollowDistance = distance;
            
            // 배열 크기 사전 계산: (추적 거리 / 기록 간격) + 여유 공간(10)
            // 예: FollowDistance 2.0 / RecordInterval 0.2 = 10. 여유분 포함 20칸이면 절대 부족하지 않습니다.
            bufferCapacity = Mathf.CeilToInt(FollowDistance / RecordInterval) + 10;
            pathRingBuffer = new float3[bufferCapacity];
            
            // 인덱스 초기화
            headIndex = 0;
            tailIndex = 0;
            bufferCount = 0;

            // 위치 초기화
            lastRecordedTargetPos = target.position;
            transform.position = target.position;
            currentWaypoint = target.position;
            hasWaypoint = false;
        }

        public void ProcessMovement(float moveSpeed, float deltaTime)
        {
            if (targetTransform == null) return;

            float3 currentTargetPos = targetTransform.position;
            float distFromLastRecord = math.distance(lastRecordedTargetPos, currentTargetPos);

            // 1. 선행자의 이동 감지 및 궤적 기록 (Ring Buffer 쓰기 - Head 이동)
            if (distFromLastRecord >= RecordInterval)
            {
                // 버퍼가 꽉 차지 않았을 때만 기록 (여유 공간을 넉넉히 잡았으므로 꽉 찰 일은 정상적인 상황에선 없습니다)
                if (bufferCount < bufferCapacity)
                {
                    pathRingBuffer[headIndex] = currentTargetPos;
                    headIndex = (headIndex + 1) % bufferCapacity; // 배열 끝에 도달하면 0으로 순환
                    bufferCount++;
                }
                lastRecordedTargetPos = currentTargetPos;
            }

            // 2. 궤적 길이 계산
            // 남은 웨이포인트 개수 * 간격 + 마지막 웨이포인트에서 현재 타겟까지의 거리
            float approximateDistance = (bufferCount * RecordInterval) + math.distance(lastRecordedTargetPos, currentTargetPos);

            // 3. JRPG 이동 로직
            if (approximateDistance > FollowDistance)
            {
                // 웨이포인트가 없고, 버퍼에 읽을 데이터가 있다면 (Ring Buffer 읽기 - Tail 이동)
                if (!hasWaypoint && bufferCount > 0)
                {
                    currentWaypoint = pathRingBuffer[tailIndex];
                    tailIndex = (tailIndex + 1) % bufferCapacity; // 데이터를 꺼낸 후 읽기 인덱스 순환
                    bufferCount--;
                    hasWaypoint = true;
                }

                if (hasWaypoint)
                {
                    float3 myPos = transform.position;
                    float3 dir = currentWaypoint - myPos;
                    float distToWaypoint = math.length(dir);
                    float moveStep = moveSpeed * deltaTime;

                    if (distToWaypoint <= moveStep)
                    {
                        transform.position = currentWaypoint;
                        hasWaypoint = false;
                    }
                    else
                    {
                        float3 moveDir = math.normalize(dir);
                        transform.position = myPos + (moveDir * moveStep);
                    }
                }
            }
        }
    }
}