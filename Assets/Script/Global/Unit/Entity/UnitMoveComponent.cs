namespace Script.Unit.Entity
{
    using Script.Field.Data;
    using Script.Map.Data;
    using Script.Map.Utility;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// [Framework] Component 계층
    /// GameObject에 부착되어 실제 이동 연산을 수행하는 최소 단위.
    /// NaviMask 기반 높이 샘플링 및 충돌 판정을 담당합니다.
    /// </summary>
    public class UnitMoveComponent : MonoBehaviour
    {
        // ── 설정값 ──────────────────────────────────────────────────────────────
        private const float MOVE_SPEED = 4f;

        /// <summary> 이동 불가 판정에 사용하는 플레이어 충돌 반경 </summary>
        private const float WALKABLE_RADIUS = 0.35f;

        /// <summary>
        /// NaviMask 높이 1단위 = 0.125f 월드 유닛.
        /// AStarBatchJobUtil.PATH_SEARCH_UNIT과 동일한 값으로, groundY 계산식과 일치.
        /// (step 9: 실제 맵 데이터로 검증 후 보정 필요 시 이 상수만 수정)
        /// </summary>
        private const float HEIGHT_STEP = 0.125f;

        // ── 상태 ─────────────────────────────────────────────────────────────────
        private UnitEntityBase _ownerEntity;
        private IMapQueryService _mapQuery;
        private Vector2 _moveInput;

        // ── 초기화 ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 소유자와 맵 조회 서비스를 주입합니다.
        /// mapQuery가 null이면 높이 보정 및 충돌 판정을 생략합니다.
        /// </summary>
        public void Initialize(UnitEntityBase owner, IMapQueryService mapQuery)
        {
            _ownerEntity = owner;
            _mapQuery = mapQuery;
        }

        // ── 입력 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// PlayerControlBrain → FieldPlayerEntity → 이 메서드 순으로 호출됩니다.
        /// input.x = 좌우(world X), input.y = 앞뒤(world Z)
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        // ── 업데이트 ──────────────────────────────────────────────────────────────

        public void ManualUpdate()
        {
            if (_moveInput == Vector2.zero) return;

            // step 10: 대각선 이동 속도 정규화 (크기 > 1이면 normalize)
            Vector2 dir = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;

            Vector3 currentPos = _ownerEntity.transform.position;
            Vector3 delta = new Vector3(dir.x, 0f, dir.y) * MOVE_SPEED * Time.deltaTime;
            Vector3 newPos = currentPos + delta;

            if (CheckWalkable(newPos))
            {
                newPos.y = SampleHeight(newPos);
                _ownerEntity.transform.position = newPos;
            }
        }

        // ── 이동 가능 판정 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 반경 WALKABLE_RADIUS 내의 서브타일을 순회하여 이동 가능 여부를 반환합니다.
        /// AStarBatchJobUtil.IsVerticeMovable과 동일한 논리를 월드 좌표계로 적용합니다.
        /// </summary>
        private bool CheckWalkable(Vector3 pos)
        {
            if (_mapQuery == null) return true;

            float2 playerXZ = new float2(pos.x, pos.z);
            float radiusSq = WALKABLE_RADIUS * WALKABLE_RADIUS;

            int tileX = Mathf.FloorToInt(pos.x);
            int tileZ = Mathf.FloorToInt(pos.z);

            // 반경 0.35f는 최대 인접 타일 1칸까지 걸침 → 3x3 범위 검사
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    // 타일 중심으로 쿼리 (floor 결과가 올바른 tile key를 가리키도록 +0.5 오프셋)
                    float3 queryPos = new float3(tileX + dx + 0.5f, pos.y, tileZ + dz + 0.5f);
                    if (!_mapQuery.TryGetTileData(queryPos, out MapTileData tile))
                        continue;

                    // 플레이어 위치를 해당 타일의 로컬 좌표(0~1 범위)로 변환
                    float2 localCenter = playerXZ - new float2(tileX + dx, tileZ + dz);

                    // 16개 서브타일 중 무효한(벽/구멍) 것과 겹치면 이동 불가
                    for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
                    {
                        if (!MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s) &&
                             MapNaviTileUtil.IsCircleOverlappingSubTile(s, localCenter, radiusSq))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // ── 높이 샘플링 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 월드 XZ 위치에서 NaviMask의 서브타일을 탐색하고,
        /// 해당 삼각형 3정점의 높이를 바리센트릭 보간하여 월드 Y를 반환합니다.
        ///
        /// groundY 공식 = tileBaseY + heightValue * HEIGHT_STEP
        /// AStarBatchJobUtil.HasLineOfSight 의 계산식과 일치합니다.
        /// </summary>
        private float SampleHeight(Vector3 pos)
        {
            if (_mapQuery == null) return pos.y;

            float3 queryPos = new float3(pos.x, pos.y, pos.z);
            if (!_mapQuery.TryGetTileData(queryPos, out MapTileData tile))
                return pos.y;

            float tileBaseY = Mathf.Floor(pos.y);
            float2 localPos = new float2(
                pos.x - Mathf.Floor(pos.x),
                pos.z - Mathf.Floor(pos.z));

            // 플레이어 XZ가 속하는 유효 서브타일 탐색
            for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
            {
                if (!MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s)) continue;

                int v0 = MapConsts.SubTileVertexMap[s * 3 + 0];
                int v1 = MapConsts.SubTileVertexMap[s * 3 + 1];
                int v2 = MapConsts.SubTileVertexMap[s * 3 + 2];

                float2 p0 = MapConsts.VertexPositions[v0];
                float2 p1 = MapConsts.VertexPositions[v1];
                float2 p2 = MapConsts.VertexPositions[v2];

                if (!IsPointInTriangle(localPos, p0, p1, p2)) continue;

                // 3정점의 높이 값 추출 (0~14)
                int h0 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v0);
                int h1 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v1);
                int h2 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v2);

                // 바리센트릭 보간으로 해당 좌표의 높이 계산
                float3 bary = BarycentricCoords(localPos, p0, p1, p2);
                float sampledHeight = (bary.x * h0 + bary.y * h1 + bary.z * h2) * HEIGHT_STEP;

                return tileBaseY + sampledHeight;
            }

            // 속하는 서브타일이 없으면 현재 Y 유지
            return pos.y;
        }

        // ── 수학 유틸 (인스턴스 없이 사용하기 위해 static) ──────────────────────────

        /// <summary> 점 p가 삼각형 (a, b, c) 내부에 있는지 외적 부호로 판별합니다. </summary>
        private static bool IsPointInTriangle(float2 p, float2 a, float2 b, float2 c)
        {
            float cp1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            float cp2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
            float cp3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
            return (cp1 >= 0f && cp2 >= 0f && cp3 >= 0f)
                || (cp1 <= 0f && cp2 <= 0f && cp3 <= 0f);
        }

        /// <summary> 삼각형 (a, b, c) 기준으로 점 p의 바리센트릭 좌표를 반환합니다. </summary>
        private static float3 BarycentricCoords(float2 p, float2 a, float2 b, float2 c)
        {
            float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (math.abs(denom) < 1e-6f)
                return new float3(1f / 3f, 1f / 3f, 1f / 3f); // 퇴화 삼각형 → 균등 분배

            float w0 = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
            float w1 = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
            float w2 = 1f - w0 - w1;
            return new float3(w0, w1, w2);
        }
    }
}
