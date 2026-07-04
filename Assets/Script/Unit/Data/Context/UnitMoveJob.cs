namespace Kompile.Data
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Kompile.Utility;

    /// <summary>
    /// [Framework] Data/Job 계층
    /// 원본의 LinkMask 기반 수직 단차 예측 및 폴백 스캔 알고리즘을 완벽히 보존하여
    /// 다중 스레드 상에서 Burst Compile로 초고속 실행되는 이동 연산 Job입니다.
    /// </summary>
    [BurstCompile]
    public struct UnitMoveJob : IJobParallelFor
    {
        private const float MOVE_SPEED = 4f;
        private const float WALKABLE_RADIUS = 0.35f;
        private const float HEIGHT_STEP = 0.125f;
        private const float DIAGONAL_SLIDE_DAMPING = 0.7071068f;
        private const float COS_45 = 0.7071068f;

        [ReadOnly] public NativeArray<float2> MoveInputs;
        [ReadOnly] public NativeArray<float3> CurrentPositions;
        [ReadOnly] public float DeltaTime;

        // MapManager가 그리드 스트리밍 시 구워내는 고속 공간 데이터 맵
        [ReadOnly] public NativeHashMap<long, BurstTileInfo> TileMap;

        [WriteOnly] public NativeArray<float3> NextPositions;

        public void Execute(int index)
        {
            float2 input = MoveInputs[index];
            float3 currentPos = CurrentPositions[index];

            if (input.x == 0f && input.y == 0f)
            {
                NextPositions[index] = currentPos;
                return;
            }

            float baseSpeed = MOVE_SPEED * DeltaTime;
            float2 dir = math.lengthsq(input) > 1f ? math.normalize(input) : input;
            float3 moveDir3D = new float3(dir.x, 0f, dir.y);

            float3 targetPos = currentPos + (moveDir3D * baseSpeed);
            float3 nextPos = currentPos;

            // 1. 의도한 주 방향 이동 검사 (호 기반 고정밀 검사)
            if (CheckWalkableArc(currentPos, targetPos, moveDir3D, WALKABLE_RADIUS, 5, math.PI * 0.5f))
            {
                nextPos = targetPos;
            }
            // 2. 대체 방향 탐색 (슬라이딩)
            else
            {
                float moveDistance = baseSpeed * DIAGONAL_SLIDE_DAMPING;
                for (int i = 0; i < 4; ++i)
                {
                    if (TryAlternativeMove(currentPos, moveDir3D, i, moveDistance, out float3 altPos))
                    {
                        nextPos = altPos;
                        break;
                    }
                }
            }

            // 3. 완벽한 3D 지형 높이 가중치 보간 계산
            if (TrySampleHeight(nextPos, out float groundY))
            {
                nextPos.y = groundY;
            }

            NextPositions[index] = nextPos;
        }

        private bool TryAlternativeMove(float3 currentPos, float3 baseDir, int index, float moveDistance, out float3 nextPos)
        {
            nextPos = currentPos;
            float cos = 0f, sin = 0f;

            switch (index)
            {
                case 0: cos = COS_45; sin = COS_45; break;
                case 1: cos = COS_45; sin = -COS_45; break;
                case 2: cos = 0f; sin = 1f; break;
                case 3: cos = 0f; sin = -1f; break;
                default: return false;
            }

            float rotX = baseDir.x * cos - baseDir.z * sin;
            float rotZ = baseDir.x * sin + baseDir.z * cos;

            float3 altDir = math.normalize(new float3(rotX, 0f, rotZ));
            float3 targetPos = currentPos + (altDir * moveDistance);

            if (CheckWalkableArc(currentPos, targetPos, altDir, WALKABLE_RADIUS, 3, math.PI * 0.25f))
            {
                nextPos = targetPos;
                return true;
            }

            return false;
        }

        private bool CheckWalkableArc(float3 curPos, float3 targetPos, float3 moveDir, float radius, int sampleCount, float maxAngleRad)
        {
            if (!IsPointWalkable(curPos, targetPos)) return false;

            if (sampleCount <= 1)
            {
                float3 pFront = targetPos + moveDir * radius;
                return IsPointWalkable(curPos, pFront);
            }

            float angleStep = (maxAngleRad * 2f) / (sampleCount - 1);

            for (int i = 0; i < sampleCount; i++)
            {
                float angle = -maxAngleRad + (angleStep * i);
                MapGeometryUtil.CalculateArcDirection(in moveDir, in angle, out float3 sampleDir);
                float3 checkPoint = targetPos + sampleDir * radius;

                if (!IsPointWalkable(curPos, checkPoint))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPointWalkable(float3 referencePos, float3 point)
        {
            int tx = (int)math.floor(point.x);
            int tz = (int)math.floor(point.z);

            // 원본의 토폴로지 구조를 그대로 경유하여 타일 획득
            if (!TryGetTileAtBurst(referencePos, tx, tz, referencePos.y, out BurstTileInfo tileInfo))
            {
                return false;
            }

            float2 localPos = new float2(point.x - tx, point.z - tz);
            return MapGeometryUtil.IsTilePointWalkable(in tileInfo.TileData, in localPos);
        }

        private bool TrySampleHeight(float3 pos, out float groundY)
        {
            groundY = pos.y;
            int tx = (int)math.floor(pos.x);
            int tz = (int)math.floor(pos.z);

            if (!TryGetTileAtBurst(pos, tx, tz, pos.y, out BurstTileInfo tileInfo))
            {
                return false;
            }

            float2 localPos = new float2(pos.x - tx, pos.z - tz);
            return MapGeometryUtil.TrySampleTileHeight(in tileInfo.TileData, in tileInfo.TileBaseY, in localPos, HEIGHT_STEP, out groundY);
        }

        /// <summary>
        /// 원본 TryGetTileAt의 LinkMask 위상 검사 알고리즘을 이식한 고속 역산 함수입니다.
        /// </summary>
        private bool TryGetTileAtBurst(float3 curPos, int targetTx, int targetTz, float referenceY, out BurstTileInfo tileInfo)
        {
            tileInfo = default;

            int curTx = (int)math.floor(curPos.x);
            int curTz = (int)math.floor(curPos.z);

            // 1. 현재 밟고 있는 타일의 데이터 Key 계산 및 조회
            float3 curQuery = new float3(curTx + 0.5f, curPos.y, curTz + 0.5f);
            MapCoordUtil.ComputeKey(curQuery, out int curGKey, out int curTKey);
            long curPackedKey = ((long)curGKey << 32) | (uint)curTKey;

            if (!TileMap.TryGetValue(curPackedKey, out BurstTileInfo curTileInfo))
            {
                return TryScanTileVerticalBurst(targetTx, targetTz, referenceY, out tileInfo);
            }

            int dx = targetTx - curTx;
            int dz = targetTz - curTz;

            // 동일 타일 내부 이동일 경우 즉시 반환
            if (dx == 0 && dz == 0)
            {
                tileInfo = curTileInfo;
                return true;
            }

            // 2. 상대 오프셋 기반 방향 인덱스 결정 (Burst 내 switch 최적화를 위해 분기 처리)
            int dirIndex = -1;
            if (dx == -1 && dz == -1) dirIndex = 0;
            else if (dx == 0 && dz == -1) dirIndex = 1;
            else if (dx == 1 && dz == -1) dirIndex = 2;
            else if (dx == 1 && dz == 0) dirIndex = 3;
            else if (dx == 1 && dz == 1) dirIndex = 4;
            else if (dx == 0 && dz == 1) dirIndex = 5;
            else if (dx == -1 && dz == 1) dirIndex = 6;
            else if (dx == -1 && dz == 0) dirIndex = 7;

            // 3. LinkMask 정보를 복원하여 다음 연결 타일의 Y 높이 오프셋을 선제 판별
            if (dirIndex != -1)
            {
                if (MapNaviTileUtil.TryGetYInt(curTileInfo.TileData.LinkMask, dirIndex, out int yOffset))
                {
                    float3 targetQuery = new float3(targetTx + 0.5f, curPos.y + yOffset, targetTz + 0.5f);
                    MapCoordUtil.ComputeKey(targetQuery, out int tgtGKey, out int tgtTKey);
                    long tgtPackedKey = ((long)tgtGKey << 32) | (uint)tgtTKey;

                    if (TileMap.TryGetValue(tgtPackedKey, out tileInfo))
                    {
                        return true;
                    }
                }
            }

            // 4. 링크 데이터가 단절되었거나 부재한 경우, 상하 수직 공간 탐색 폴백 작동
            return TryScanTileVerticalBurst(targetTx, targetTz, referenceY, out tileInfo);
        }

        /// <summary>
        /// 힙 할당 없이 상하 수직 레이어를 순회하며 유효 타일을 식별합니다. (Primitive 값 형식 매개변수 `in` 제거)
        /// </summary>
        private bool TryScanTileVerticalBurst(int tx, int tz, float referenceY, out BurstTileInfo tileInfo)
        {
            tileInfo = default;

            // 0f -> -1f -> 1f 오프셋 순회
            for (int i = 0; i < 3; i++)
            {
                float yOff = i == 0 ? 0f : (i == 1 ? -1f : 1f);
                float3 testQuery = new float3(tx + 0.5f, referenceY + yOff, tz + 0.5f);

                MapCoordUtil.ComputeKey(testQuery, out int gKey, out int tKey);
                long packedKey = ((long)gKey << 32) | (uint)tKey;

                if (TileMap.TryGetValue(packedKey, out tileInfo))
                {
                    return true;
                }
            }
            return false;
        }
    }
}