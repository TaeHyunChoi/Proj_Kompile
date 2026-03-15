namespace Script.Field.Entity.Component
{
    using UnityEngine;
    using Unity.Mathematics;
    using Script.Map.Data;
    using Script.Map.Utility;
    using Script.Map.Provider;

    /// <summary>
    /// [Framework] Component: 게임 오브젝트에 부속되어 이동 기능을 수행하는 최소 단위.
    /// 외부(Manager)로부터 Input을 받아, MapRepoProvider의 데이터를 기준으로 충돌 및 높이를 계산하여 최종 이동을 수행합니다.
    /// </summary>
    public class PlayerMoveComponent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("엔티티의 충돌 판정 반경입니다. 서브 타일 충돌 검사에 사용됩니다.")]
        public float ColliderRadius = 0.25f;

        // 이동 상태 및 설정값을 담은 순수 Data (값 중심 자료구조 지향)
        // 10f의 속도와 조향 힘(Steering)을 기본으로 세팅합니다.
        private MovementContext moveContext;
        
        // 맵 데이터를 조회하기 위한 Provider 캐싱
        private MapRepoProvider mapProvider;

        /// <summary>
        /// Manager가 Entity를 생성/초기화할 때 호출하여 의존성을 주입합니다.
        /// </summary>
        public void Initialize(MapRepoProvider provider)
        {
            mapProvider = provider;
            
            // MovementContext 초기화 (Time.deltaTime * 10f 정도의 속도를 상정)
            moveContext = new MovementContext()
            {
                MaxSpeed = 10f,
                SteeringForce = 15f,
                CurrentVelocity = float3.zero
            };
        }

        /// <summary>
        /// 매 프레임 Manager로부터 호출되어 실제 이동 로직을 수행합니다.
        /// </summary>
        /// <param name="inputDirection">정규화된 입력 방향 (x, z)</param>
        /// <param name="deltaTime">Time.deltaTime</param>
        public void ProcessMovement(float2 inputDirection, float deltaTime)
        {
            if (mapProvider == null) return;

            float3 currentPos = transform.position;

            // 1. 목표 속도 및 조향(Steering) 힘 계산
            // 관성을 부드럽게 처리하기 위해 현재 속도에 조향 힘을 누적합니다.
            float3 desiredVelocity = new float3(inputDirection.x, 0f, inputDirection.y) * moveContext.MaxSpeed;
            float3 steering = (desiredVelocity - moveContext.CurrentVelocity) * moveContext.SteeringForce;
            moveContext.CurrentVelocity += steering * deltaTime;

            // 만약 속도가 거의 0이라면 연산을 스킵합니다.
            if (math.lengthsq(moveContext.CurrentVelocity) < 0.001f)
            {
                moveContext.CurrentVelocity = float3.zero;
                return;
            }

            // 2. 이동할 다음 위치 계산 (벽 타기(Sliding) 로직 포함)
            float3 nextPos = currentPos + moveContext.CurrentVelocity * deltaTime;

            // 대각선 이동 시 한쪽 벽이 막히면 미끄러지듯 이동하도록 분리 검사합니다.
            if (!IsValidPosition(nextPos))
            {
                // X축 단독 이동 시도
                float3 nextPosX = currentPos + new float3(moveContext.CurrentVelocity.x, 0, 0) * deltaTime;
                if (IsValidPosition(nextPosX))
                {
                    nextPos = nextPosX;
                    moveContext.CurrentVelocity.z = 0; // Z축 속도 상실
                }
                else
                {
                    // Z축 단독 이동 시도
                    float3 nextPosZ = currentPos + new float3(0, 0, moveContext.CurrentVelocity.z) * deltaTime;
                    if (IsValidPosition(nextPosZ))
                    {
                        nextPos = nextPosZ;
                        moveContext.CurrentVelocity.x = 0; // X축 속도 상실
                    }
                    else
                    {
                        // 양쪽 다 막혔다면 정지
                        nextPos = currentPos;
                        moveContext.CurrentVelocity = float3.zero;
                    }
                }
            }

            // 3. 최종 확정된 X, Z 위치에 따른 Y(높이)값 계산
            nextPos.y = CalculateHeightAtPosition(nextPos);

            // 4. Transform 갱신 (Entity 실제 이동)
            transform.position = nextPos;
        }

        /// <summary>
        /// 타일의 유효성 및 원형 충돌 범위(ColliderRadius)를 체크합니다.
        /// </summary>
        private bool IsValidPosition(float3 targetPos)
        {
            // 타일 ID 도출 (Burst 최적화 유틸 사용)
            long tileId = MapCoordUtil.ComputeTileID(targetPos);
            
            // MapRepoProvider에 타일 데이터가 없으면 갈 수 없는 곳(벽/낭떠러지)
            if (!mapProvider.TileDic.TryGetValue(tileId, out MapTileData tileData))
            {
                return false;
            }

            // 타일 내 로컬 좌표 계산 (0.0f ~ 1.0f)
            float2 localPos = new float2(
                targetPos.x - math.floor(targetPos.x),
                targetPos.z - math.floor(targetPos.z)
            );

            float radiusSq = ColliderRadius * ColliderRadius;

            // 16개의 서브 타일(삼각형)을 모두 순회하며 '유효하지 않은 타일(구멍/벽)'과 겹치는지 검사
            for (int sIdx = 0; sIdx < MapConsts.TRIANGLES_COUNT; sIdx++)
            {
                // IsSubTileValid가 false면 밟을 수 없는 영역입니다.
                if (!MapNaviTileUtil.IsSubTileValid(tileData.NaviMask, sIdx))
                {
                    // 갈 수 없는 영역의 서브 타일과 플레이어의 반경이 겹친다면 충돌!
                    if (MapNaviTileUtil.IsCircleOverlappingSubTile(sIdx, localPos, radiusSq))
                    {
                        return false; 
                    }
                }
            }

            return true; // 어떤 막힌 지형과도 겹치지 않음
        }

        /// <summary>
        /// 현재 좌표가 속한 서브 타일(삼각형)의 정점 높이를 기반으로 Y값을 계산합니다.
        /// </summary>
        private float CalculateHeightAtPosition(float3 targetPos)
        {
            long tileId = MapCoordUtil.ComputeTileID(targetPos);
            if (!mapProvider.TileDic.TryGetValue(tileId, out MapTileData tileData))
            {
                return targetPos.y; // 맵 밖이면 현재 y 유지
            }

            float2 localPos = new float2(
                targetPos.x - math.floor(targetPos.x),
                targetPos.z - math.floor(targetPos.z)
            );

            // 현재 엔티티의 중심점(반경 0)이 속해있는 서브 타일 인덱스를 찾습니다.
            int currentSubTileIndex = -1;
            for (int sIdx = 0; sIdx < MapConsts.TRIANGLES_COUNT; sIdx++)
            {
                if (MapNaviTileUtil.IsCircleOverlappingSubTile(sIdx, localPos, 0f))
                {
                    currentSubTileIndex = sIdx;
                    break;
                }
            }

            // 만약 유효한 서브 타일을 찾지 못했다면 이전 높이 유지
            if (currentSubTileIndex == -1 || !MapNaviTileUtil.IsSubTileValid(tileData.NaviMask, currentSubTileIndex))
            {
                return targetPos.y;
            }

            // 해당 서브 타일을 구성하는 3개의 정점 인덱스 확보
            int vIdx0 = MapConsts.SubTileVertexMap[currentSubTileIndex * 3 + 0];
            int vIdx1 = MapConsts.SubTileVertexMap[currentSubTileIndex * 3 + 1];
            int vIdx2 = MapConsts.SubTileVertexMap[currentSubTileIndex * 3 + 2];

            // NaviMask에서 각 정점의 높이 정보(0~15) 추출
            int h0 = MapNaviTileUtil.GetHeightFromNaviMask(tileData.NaviMask, vIdx0);
            int h1 = MapNaviTileUtil.GetHeightFromNaviMask(tileData.NaviMask, vIdx1);
            int h2 = MapNaviTileUtil.GetHeightFromNaviMask(tileData.NaviMask, vIdx2);

            // 3 정점 높이의 평균값(보간)을 구합니다. 
            // (추후 필요하시다면 정밀한 Barycentric Interpolation 로직으로 교체 가능합니다.)
            float avgMaskInt = (h0 + h1 + h2) / 3f;

            // 실제 월드 좌표 계산 시, 타일의 원점(Pivot) 위치를 가져와 높이를 더해줍니다.
            // 높이는 1단위당 0.125f (TryGetVerticeHeight 유틸 기준)
            MapCoordUtil.ComputeWorldPosition(tileId, out float3 pivotPos);
            float finalY = pivotPos.y + (avgMaskInt * 0.125f);

            return finalY;
        }
    }
}