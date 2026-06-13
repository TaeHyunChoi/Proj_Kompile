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
    /// 입력 방향 기준 대안 방향들을 탐색하여 부드러운 2.5D 슬라이딩을 구현하는 컴포넌트입니다.
    /// </summary>
    public class UnitMoveComponent : MonoBehaviour
    {
        private const float MOVE_SPEED = 4f;
        private const float WALKABLE_RADIUS = 0.35f;
        private const float HEIGHT_STEP = 0.125f;

        private const float DIAGONAL_SLIDE_DAMPING = 0.7071068f; 
        private const float COS_45 = 0.7071068f;

        private UnitEntityBase _ownerEntity;
        private IMapQueryService _mapQuery;
        private Vector2 _moveInput;

        public void Initialize(UnitEntityBase owner, IMapQueryService mapQuery)
        {
            _ownerEntity = owner;
            _mapQuery = mapQuery;
        }

        private static readonly float[] DiagonalAngleDegree = new[] { 45f, -45f, 90f, -90f };
        
        public void UpdateIntent(in UnitIntent intent)
        {
            if (!_ownerEntity) return;                
            
            _moveInput = intent.MoveInput;
            if (Vector2.zero == _moveInput) return;

            float baseSpeed = MOVE_SPEED * Time.deltaTime;
    
            Vector2 dir = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
            float3 moveDir3D = new float3(dir.x, 0f, dir.y);
            Vector3 currentPos = _ownerEntity.transform.position;

            Vector3 targetPos = currentPos + (Vector3)(moveDir3D * baseSpeed);
            Vector3 nextPos;

            // 의도한 주 방향 이동: 캐릭터 반경 전체 수호를 위해 호(Arc) 기반 고정밀 검사 수행
            if (CheckWalkableArc(targetPos, moveDir3D, WALKABLE_RADIUS, sampleCount: 5, maxAngleRad: math.PI * 0.5f))
            {
                currentPos = targetPos;
            }
            // 대체 방향 탐색 (슬라이딩)
            else
            {
                float moveDistance = baseSpeed * DIAGONAL_SLIDE_DAMPING;
                for (int i = 0; i < DiagonalAngleDegree.Length; ++i)
                {
                    if (TryAlternativeMove(currentPos, moveDir3D, i, moveDistance, out nextPos))
                    {
                        currentPos = nextPos;
                        break;
                    }
                }
            }

            if (TrySampleHeight(currentPos, out float groundY))
            {
                currentPos.y = groundY;
            }

            _ownerEntity.transform.position = currentPos;
        }
        
        private bool TryAlternativeMove(Vector3 currentPos, float3 baseDir, int index, float moveDistance, out Vector3 nextPos)
        {
            nextPos = currentPos;
            float cos = 0f, sin = 0f;

            switch (index)
            {
                case 0: cos = COS_45; sin = COS_45;  break;
                case 1: cos = COS_45; sin = -COS_45; break;
                case 2: cos = 0f; sin = 1f; break;
                case 3: cos = 0f; sin = -1f; break;
                default: return false;
            }
            
            float rotX = baseDir.x * cos - baseDir.z * sin;
            float rotZ = baseDir.x * sin + baseDir.z * cos;

            float3 altDir = math.normalize(new float3(rotX, 0f, rotZ));
            Vector3 targetPos = currentPos + (Vector3)(altDir * moveDistance);

            if (CheckWalkableArc(targetPos, altDir, WALKABLE_RADIUS, sampleCount: 3, maxAngleRad: math.PI * 0.25f))
            {
                nextPos = targetPos;
                return true;
            }

            return false;
        }

        private bool CheckWalkableArc(Vector3 targetPos, float3 moveDir, float radius, int sampleCount, float maxAngleRad)
        {
            if (!IsPointWalkable(targetPos, (float3)targetPos)) 
                return false;

            if (sampleCount <= 1)
            {
                float3 pFront = (float3)targetPos + moveDir * radius;
                return IsPointWalkable(targetPos, pFront);
            }

            float angleStep = (maxAngleRad * 2f) / (sampleCount - 1); 

            for (int i = 0; i < sampleCount; i++)
            {
                float angle = -maxAngleRad + (angleStep * i);
                
                // 💡 Utility 계층의 BurstCompile된 고속 방향 연산 대리 호출
                MapGeometryUtil.CalculateArcDirection(in moveDir, in angle, out float3 sampleDir);
                float3 checkPoint = (float3)targetPos + sampleDir * radius;

                if (!IsPointWalkable(targetPos, checkPoint))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPointWalkable(Vector3 referencePos, float3 point)
        {
            if (null == _mapQuery) return true;

            int tx = Mathf.FloorToInt(point.x);
            int tz = Mathf.FloorToInt(point.z);

            // 로우레벨 탐색 컨텍스트(인터페이스 참조 영역)
            if (!TryGetTileAt(tx, tz, referencePos.y, out MapTileData tile, out _))
            {
                return false;
            }

            float2 localPos = new float2(point.x - tx, point.z - tz);
            
            // 💡 빽빽한 삼각형 포함 루프 연산을 Utility의 BurstCompile 메서드로 전량 위임
            return MapGeometryUtil.IsTilePointWalkable(in tile, in localPos);
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

            // 💡 무거운 삼각면 가중치 보간 연산 루프를 Utility의 BurstCompile 메서드로 전량 위임
            return MapGeometryUtil.TrySampleTileHeight(in tile, in tileBaseY, in localPos, HEIGHT_STEP, out groundY);
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
                return TryScanTileVertical(in targetTx, in targetTz, in referenceY, out tile, out tileBaseY);
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
                (-1, -1) => 0, (0, -1) => 1, (1, -1) => 2,
                (1, 0) => 3,  (1, 1) => 4,  (0, 1) => 5,
                (-1, 1) => 6, (-1, 0) => 7, _ => -1
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

            return TryScanTileVertical(in targetTx, in targetTz, in referenceY, out tile, out tileBaseY);
        }

        private static readonly float[] yOffsets = { 0f, -1f, 1f };
        private bool TryScanTileVertical(in int tx, in int tz, in float referenceY, out MapTileData tile, out float tileBaseY)
        {
            tile = default;
            tileBaseY = 0f;
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