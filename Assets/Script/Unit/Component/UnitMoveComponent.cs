namespace Kompile.Unit.Component
{
    using Kompile.Field.Data;
    using Kompile.Map.Data;
    using Kompile.Map.Utility;
    using Kompile.Unit.Data;
    using Kompile.Unit.Entity;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// [Framework] Component 계층
    /// 입력 방향 기준 +-90도 내 대안 방향들을 우선순위(대각선->직교)에 따라 탐색하여
    /// 완벽하고 부드러운 2.5D 슬라이딩 감각을 구현하는 컴포넌트입니다.
    /// </summary>
    public class UnitMoveComponent : MonoBehaviour
    {
        private const float MOVE_SPEED = 4f;
        private const float WALKABLE_RADIUS = 0.35f;
        private const float HEIGHT_STEP = 0.125f;

        // 💡 조작감 미세 조정을 위한 슬라이딩 감속 상수 (상수로 격리하여 커스텀 용이)
        private const float DIAGONAL_SLIDE_DAMPING = 0.7071068f; // 45도 경사 미끄러짐 가중치 (Cos 45°)
        private const float ORTHOGONAL_SLIDE_DAMPING = 0.5f;     // 90도 직교 벽면 마찰 마찰력 모사 가중치
        private const float COS_45 = 0.7071068f;

        private UnitEntityBase _ownerEntity;
        private IMapQueryService _mapQuery;
        private Vector2 _moveInput;

        public void Initialize(UnitEntityBase owner, IMapQueryService mapQuery)
        {
            _ownerEntity = owner;
            _mapQuery = mapQuery;
        }

        public void UpdateIntent(in UnitIntent intent)
        {
            if (null == _ownerEntity) return;

            _moveInput = intent.MoveInput;
            if (_moveInput == Vector2.zero) return;

            // 1. 입력 축 정규화 및 원본 프레임 이동 속도 연산
            Vector2 dir = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
            float baseSpeed = MOVE_SPEED * Time.deltaTime;

            float3 moveDir3D = new float3(dir.x, 0f, dir.y);
            Vector3 currentPos = _ownerEntity.transform.position;

            // 2. [나으리의 우선순위 알고리즘 이식] 5단계 전방 방향성 탐색 체인
            bool hasMoved = false;

            // [우선순위 1] 원래 가려던 전방 메인 방향 검증 (100% 속도)
            Vector3 targetPos = currentPos + (Vector3)(moveDir3D * baseSpeed);
            if (CheckWalkableWithVelocity(targetPos, moveDir3D))
            {
                currentPos = targetPos;
                hasMoved = true;
            }

            // [우선순위 2] 좌측 45도 대각선 방향 탐색 (동 -> 동북)
            if (!hasMoved && TryAlternativeMove(currentPos, moveDir3D, 45f, baseSpeed * DIAGONAL_SLIDE_DAMPING, out Vector3 nextPos))
            {
                currentPos = nextPos;
                hasMoved = true;
            }

            // [우선순위 3] 우측 45도 대각선 방향 탐색 (동 -> 동남)
            if (!hasMoved && TryAlternativeMove(currentPos, moveDir3D, -45f, baseSpeed * DIAGONAL_SLIDE_DAMPING, out nextPos))
            {
                currentPos = nextPos;
                hasMoved = true;
            }

            // [우선순위 4] 좌측 90도 완전 직교 방향 탐색 (동 -> 북)
            if (!hasMoved && TryAlternativeMove(currentPos, moveDir3D, 90f, baseSpeed * ORTHOGONAL_SLIDE_DAMPING, out nextPos))
            {
                currentPos = nextPos;
                hasMoved = true;
            }

            // [우선순위 5] 우측 90도 완전 직교 방향 탐색 (동 -> 남)
            if (!hasMoved && TryAlternativeMove(currentPos, moveDir3D, -90f, baseSpeed * ORTHOGONAL_SLIDE_DAMPING, out nextPos))
            {
                currentPos = nextPos;
            }

            // 3. 최종 확정된 평면 좌표 위에서 완벽한 3D 지형 높이 보간 결합
            if (TrySampleHeight(currentPos, out float groundY))
            {
                currentPos.y = groundY;
            }

            _ownerEntity.transform.position = currentPos;
        }

        /// <summary>
        /// 원래 방향 벡터를 각도만큼 회전시켜 대안 변위를 시뮬레이션하고 지형 안전성을 검증합니다. (Zero GC)
        /// </summary>
        private bool TryAlternativeMove(Vector3 currentPos, float3 baseDir, float angleDeg, float moveDistance, out Vector3 nextPos)
        {
            nextPos = currentPos;

            // 삼각함수 호출 힙 부하를 방지하기 위한 하드코딩 회전 행렬 매핑
            float cos = 0f;
            float sin = 0f;

            if (angleDeg == 45f) { cos = COS_45; sin = COS_45; }
            else if (angleDeg == -45f) { cos = COS_45; sin = -COS_45; }
            else if (angleDeg == 90f) { cos = 0f; sin = 1f; }
            else if (angleDeg == -90f) { cos = 0f; sin = -1f; }

            // 3D XZ 평면 회전 수식
            float rotX = baseDir.x * cos - baseDir.z * sin;
            float rotZ = baseDir.x * sin + baseDir.z * cos;

            float3 altDir = math.normalize(new float3(rotX, 0f, rotZ));
            Vector3 targetPos = currentPos + (Vector3)(altDir * moveDistance);

            if (CheckWalkableWithVelocity(targetPos, altDir))
            {
                nextPos = targetPos;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 목적지 좌표를 기준으로, 진행하는 선단 에지(Leading Edge)에 반원 호 모양의 3개 프롭 포인트를 심어 지형을 다이렉트 검증합니다.
        /// 이 방식으로 사각형 박스 충돌 연산 특유의 내부 유령 모서리 걸림 문제를 완전히 해소합니다.
        /// </summary>
        private bool CheckWalkableWithVelocity(Vector3 targetPos, float3 moveDir)
        {
            // 중심점 베이스 라인 검사
            if (!IsPointWalkable(targetPos, (float3)targetPos)) return false;

            // 정전방 프롭 포인트 샘플링
            float3 pFront = (float3)targetPos + moveDir * WALKABLE_RADIUS;
            if (!IsPointWalkable(targetPos, pFront)) return false;

            // 전방 좌측 45도 프롭 포인트 샘플링
            float3 pLeft = (float3)targetPos + RotateQuarterVector(moveDir, true) * WALKABLE_RADIUS;
            if (!IsPointWalkable(targetPos, pLeft)) return false;

            // 전방 우측 45도 프롭 포인트 샘플링
            float3 pRight = (float3)targetPos + RotateQuarterVector(moveDir, false) * WALKABLE_RADIUS;
            if (!IsPointWalkable(targetPos, pRight)) return false;

            return true;
        }

        private float3 RotateQuarterVector(float3 dir, bool left)
        {
            float sin = left ? COS_45 : -COS_45;
            float cos = COS_45;
            return new float3(dir.x * cos - dir.z * sin, 0f, dir.x * sin + dir.z * cos);
        }

        private bool IsPointWalkable(Vector3 referencePos, float3 point)
        {
            if (null == _mapQuery) return true;

            int tx = Mathf.FloorToInt(point.x);
            int tz = Mathf.FloorToInt(point.z);

            // 타일이 없는 허공 격자 낭떠러지 즉시 예외 컷
            if (!TryGetTileAt(tx, tz, referencePos.y, out MapTileData tile, out _))
            {
                return false;
            }

            float2 localPos = new float2(point.x - tx, point.z - tz);

            // 16개 서브타일 삼각면 매핑 레이어 서치 후 최종 유효성 비트 연산 바인딩
            for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
            {
                int v0 = MapConsts.SubTileVertexMap[s * 3 + 0];
                int v1 = MapConsts.SubTileVertexMap[s * 3 + 1];
                int v2 = MapConsts.SubTileVertexMap[s * 3 + 2];

                float2 p0 = MapConsts.VertexPositions[v0];
                float2 p1 = MapConsts.VertexPositions[v1];
                float2 p2 = MapConsts.VertexPositions[v2];

                if (MapGeometryUtil.IsPointInTriangle(localPos, p0, p1, p2))
                {
                    return MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s);
                }
            }

            return false;
        }

        private bool TrySampleHeight(Vector3 pos, out float groundY)
        {
            groundY = pos.y;
            if (null == _mapQuery) return false;

            int targetTx = Mathf.FloorToInt(pos.x);
            int targetTz = Mathf.FloorToInt(pos.z);

            if (!TryGetTileAt(targetTx, targetTz, pos.y, out MapTileData tile, out float tileBaseY))
            {
                return false;
            }

            float2 localPos = new float2(pos.x - targetTx, pos.z - targetTz);

            for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
            {
                if (!MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s)) continue;

                int v0 = MapConsts.SubTileVertexMap[s * 3 + 0];
                int v1 = MapConsts.SubTileVertexMap[s * 3 + 1];
                int v2 = MapConsts.SubTileVertexMap[s * 3 + 2];

                float2 p0 = MapConsts.VertexPositions[v0];
                float2 p1 = MapConsts.VertexPositions[v1];
                float2 p2 = MapConsts.VertexPositions[v2];

                if (!MapGeometryUtil.IsPointInTriangle(localPos, p0, p1, p2)) continue;

                int h0 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v0);
                int h1 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v1);
                int h2 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v2);

                float3 bary = MapGeometryUtil.BarycentricCoords(localPos, p0, p1, p2);
                float sampledHeight = (bary.x * h0 + bary.y * h1 + bary.z * h2) * HEIGHT_STEP;

                groundY = tileBaseY + sampledHeight;
                return true;
            }
            return false;
        }

        private bool TryGetTileAt(int targetTx, int targetTz, float referenceY, out MapTileData tile, out float tileBaseY)
        {
            tile = default;
            tileBaseY = 0f;
            if (null == _mapQuery) return false;

            Vector3 curPos = _ownerEntity.transform.position;
            int curTx = Mathf.FloorToInt(curPos.x);
            int curTz = Mathf.FloorToInt(curPos.z);

            float3 curQuery = new float3(curTx + 0.5f, curPos.y, curTz + 0.5f);
            if (!_mapQuery.TryGetTileData(curQuery, out MapTileData curTile))
            {
                return TryScanTileVertical(targetTx, targetTz, referenceY, out tile, out tileBaseY);
            }

            int dx = targetTx - curTx;
            int dz = targetTz - curTz;

            if (dx == 0 && dz == 0)
            {
                tile = curTile;
                MapCoordUtil.ComputeKey(curQuery, out int gKey, out int tKey);
                MapCoordUtil.GetPivot(gKey, tKey, out float3 pivot);
                tileBaseY = pivot.y;
                return true;
            }

            int dirIndex = (dx, dz) switch
            {
                (-1, -1) => 0,
                (0, -1) => 1,
                (1, -1) => 2,
                (1, 0) => 3,
                (1, 1) => 4,
                (0, 1) => 5,
                (-1, 1) => 6,
                (-1, 0) => 7,
                _ => -1
            };

            if (dirIndex != -1)
            {
                if (MapNaviTileUtil.TryGetYInt(curTile.LinkMask, dirIndex, out int yOffset))
                {
                    float3 targetQuery = new float3(targetTx + 0.5f, curPos.y + yOffset, targetTz + 0.5f);
                    if (_mapQuery.TryGetTileData(targetQuery, out tile))
                    {
                        MapCoordUtil.ComputeKey(targetQuery, out int gKey, out int tKey);
                        MapCoordUtil.GetPivot(gKey, tKey, out float3 pivot);
                        tileBaseY = pivot.y;
                        return true;
                    }
                }
            }

            return TryScanTileVertical(targetTx, targetTz, referenceY, out tile, out tileBaseY);
        }

        private bool TryScanTileVertical(int tx, int tz, float referenceY, out MapTileData tile, out float tileBaseY)
        {
            tile = default;
            tileBaseY = 0f;

            float[] yOffsets = { 0f, -1f, 1f };
            for (int i = 0; i < yOffsets.Length; i++)
            {
                float3 testQuery = new float3(tx + 0.5f, referenceY + yOffsets[i], tz + 0.5f);
                if (_mapQuery.TryGetTileData(testQuery, out tile))
                {
                    MapCoordUtil.ComputeKey(testQuery, out int gKey, out int tKey);
                    MapCoordUtil.GetPivot(gKey, tKey, out float3 pivot);
                    tileBaseY = pivot.y;
                    return true;
                }
            }
            return false;
        }
    }
}