namespace Kompile.Entities
{
    using UnityEngine;
    using Unity.Mathematics;
    using Data;
    using Domain;
    using Utility;

    public class ActorFieldMoveComponent
    {
        private const float MOVE_SPEED = 4f;
        private const float WALKABLE_RADIUS = 0.35f;
        private const float HEIGHT_STEP = 0.125f;
        private const float DIAGONAL_SLIDE_DAMPING = 0.7071068f;
        private const float COS_45 = 0.7071068f;

        private Transform _transform;
        private MapProvider _mapProvider;

        public void Initialize(Transform transform, MapProvider mapProvider)
        {
            _transform   = transform;
            _mapProvider = mapProvider;
        }
        
        public void OnUpdate(float2 input, float deltaTime)
        {
            if (null == _mapProvider || !_mapProvider.NativeTileMap.IsCreated)
            {
                return;
            }
            
            if (input.x == 0f && input.y == 0f)
            { 
                return;
            }

            float3 currentPos = _transform.position;
            float baseSpeed = MOVE_SPEED * deltaTime;
            float2 dir = math.lengthsq(input) > 1f ? math.normalize(input) : input;
            float3 moveDir3D = new float3(dir.x, 0f, dir.y);

            float3 targetPos = currentPos + (moveDir3D * baseSpeed);
            float3 nextPos = currentPos;

            // 의도한 방향으로 이동이 가능한지 확인
            if (CheckWalkableArc(currentPos, targetPos, moveDir3D, WALKABLE_RADIUS, 5, math.PI * 0.5f))
            {
                nextPos = targetPos;
            }
            // 대체 방향을 탐색
            else
            {
                float moveDistance = baseSpeed * DIAGONAL_SLIDE_DAMPING;
                for (int i = 0; i < 4; ++i)
                {
                    if (TryAlernativeMove(currentPos, moveDir3D, i, moveDistance, out float3 altPos))
                    {
                        nextPos = altPos;
                        break;
                    }
                }
            }

            // 지형 높이 샘플링
            if (TrySampleHeight(nextPos, out float groudY))
            {
                nextPos.y = groudY;
            }

            _transform.position = nextPos;
        }

        private bool TryAlernativeMove(float3 currentPos, float3 baseDir, int index, float moveDistance, out float3 nextPos)
        {
            nextPos = currentPos;
            float cos = 0f, sin = 0f;

            switch (index)
            {
                case 0: cos = COS_45; sin = COS_45; break;
                case 1: cos = COS_45; sin = -COS_45; break;
                case 2: cos = 0f; sin = 1f; break;
                case 3: cos = 0f; sin = -1f; break;
                default:
                    return false;
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

        private bool TrySampleHeight(float3 pos, out float groudY)
        {
            groudY = pos.y;
            int tx = (int)math.floor(pos.x);
            int tz = (int)math.floor(pos.z);

            if (!TryGetTile(pos, tx, tz, pos.y, out MapTileInfo tileInfo))
            {
                return false;
            }

            float2 localPos = new float2(pos.x - tx, pos.z - tz);
            return InUtilMapGeometry.TrySampleTileHeight(in tileInfo.TileData, tileInfo.TileBaseY, in localPos, HEIGHT_STEP, out groudY);
        }

        private bool CheckWalkableArc(float3 curPos, float3 targetPos, float3 moveDir, float radius, int sampleCount, float maxAngleRad)
        { 
            if (!IsPointWalkable(curPos, targetPos))
            {
                return false;
            }
            if (sampleCount <= 1)
            {
                float3 pFront = targetPos + moveDir * radius;
                return IsPointWalkable(curPos, pFront);
            }

            float angleStep = (maxAngleRad * 2f) / (sampleCount - 1);
            for (int i = 0; i < sampleCount; ++i)
            {
                float angle = -maxAngleRad + (angleStep * i);
                InUtilMapGeometry.CalculateArcDirection(in moveDir, in angle, out float3 sampleDir);
                float3 checkPoint = targetPos + sampleDir * radius;

                if(!IsPointWalkable(curPos, checkPoint))
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

            if (!TryGetTile(referencePos, tx, tz, referencePos.y, out MapTileInfo tileInfo))
            {
                return false;
            }

            float2 localPos = new float2(point.x - tx, point.z - tz);
            return InUtilMapGeometry.IsTilePointWalkable(in tileInfo.TileData, in localPos);
        }

        private bool TryGetTile(float3 curPos, int targetTx, int targetTz, float referenceY, out MapTileInfo tileInfo)
        {
            tileInfo = default;

            int curTx = (int)math.floor(curPos.x);
            int curTz = (int)math.floor(curPos.z);

            float3 curQuery = new float3(curTx + 0.5f, curPos.y, curTz + 0.5f);
            InUtilMapKey.ComputeKey(curQuery, out int curGKey, out int curTKey);
            // [수정] 상위 32비트를 curGKey로 수정
            long curPackedKey = ((long)curGKey << 32) | (uint)curTKey;

            var tileMap = _mapProvider.NativeTileMap;
            if (!tileMap.TryGetValue(curPackedKey, out MapTileInfo curTileInfo))
            {
                return TryScanTileVertical(targetTx, targetTz, referenceY, out tileInfo);
            }

            int dx = targetTx - curTx;
            int dz = targetTz - curTz;

            if (0 == dx && dz == 0)
            {
                tileInfo = curTileInfo;
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

            if (-1 != dirIndex
                && InUtilMapNavi.TryGetYInt(curTileInfo.TileData.LinkMask, dirIndex, out int yOffset))
            {
                float3 targetQuery = new float3(targetTx + 0.5f, curPos.y + yOffset, targetTz + 0.5f);
                InUtilMapKey.ComputeKey(targetQuery, out int tgtGkey, out int tgtTKey);
                long tgtPackedKey = ((long)tgtGkey << 32) | (uint)tgtTKey;

                if (tileMap.TryGetValue(tgtPackedKey, out tileInfo))
                {
                    return true;
                }
            }

            return TryScanTileVertical(targetTx, targetTz, referenceY, out tileInfo);
        }

        private bool TryScanTileVertical(int tx, int tz, float referenceY, out MapTileInfo tileInfo)
        {
            tileInfo = default;
            var tileMap = _mapProvider.NativeTileMap;

            for (int i = 0; i < 3; ++i)
            {
                float yOff = (0 == i) ? 0f : (1 == i) ? -1f : 1f;
                float3 testQuery = new float3(tx + 0.5f, referenceY + yOff, tz + 0.5f);
                InUtilMapKey.ComputeKey(testQuery, out int gKey, out int tKey);
                long packedKey = ((long)gKey << 32) | (uint)tKey;
                if (tileMap.TryGetValue(packedKey, out tileInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            _transform = null;
            _mapProvider = null;
        }
    }
}