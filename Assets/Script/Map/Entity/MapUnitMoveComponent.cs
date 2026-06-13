//namespace Kompile.Unit.Component
//{
//    using Kompile.Field.Data;
//    using Kompile.Map.Data;
//    using Kompile.Map.Utility;
//    using Kompile.Unit.Data;
//    using Kompile.Unit.Entity;
//    using Unity.Mathematics;
//    using UnityEngine;

//    /// <summary>
//    /// [Framework] Component 계층
//    /// GameObject에 부착되어 실제 이동 연산을 수행하는 최소 단위.
//    /// NaviMask 기반 높이 샘플링 및 슬라이딩 충돌 판정을 담당합니다.
//    /// </summary>
//    public class MapUnitMoveComponent : MonoBehaviour
//    {
//        private const float MOVE_SPEED = 4f;
//        private const float WALKABLE_RADIUS = 0.35f;
//        private const float HEIGHT_STEP = 0.125f;

//        private UnitEntityBase _ownerEntity;
//        private IMapQueryService _mapQuery;
//        private Vector2 _moveInput;

//        public void Initialize(UnitEntityBase owner, IMapQueryService mapQuery)
//        {
//            _ownerEntity = owner;
//            _mapQuery = mapQuery;
//        }

//        public void UpdateIntent(in UnitIntent intent)
//        {
//            if (null == _ownerEntity) return;

//            _moveInput = intent.MoveInput;
//            if (_moveInput == Vector2.zero) return;

//            Vector2 dir = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;

//            Vector3 currentPos = _ownerEntity.transform.position;
//            Vector3 delta = (MOVE_SPEED * Time.deltaTime) * new Vector3(dir.x, 0f, dir.y);
//            Vector3 targetPos = currentPos + delta;

//            if (CheckWalkable(targetPos))
//            {
//                currentPos = targetPos;
//            }
//            else
//            {
//                Vector3 testX = currentPos + new Vector3(delta.x, 0f, 0f);
//                if (CheckWalkable(testX)) currentPos.x = testX.x;

//                Vector3 testZ = currentPos + new Vector3(0f, 0f, delta.z);
//                if (CheckWalkable(testZ)) currentPos.z = testZ.z;
//            }

//            if (TrySampleHeight(currentPos, out float groundY))
//            {
//                currentPos.y = groundY;
//            }

//            _ownerEntity.transform.position = currentPos;
//        }

//        private bool CheckWalkable(Vector3 pos)
//        {
//            if (null == _mapQuery) return true;

//            float2 playerXZ = new float2(pos.x, pos.z);
//            float radiusSq = WALKABLE_RADIUS * WALKABLE_RADIUS;

//            int tileX = Mathf.FloorToInt(pos.x);
//            int tileZ = Mathf.FloorToInt(pos.z);

//            for (int dx = -1; dx <= 1; dx++)
//            {
//                for (int dz = -1; dz <= 1; dz++)
//                {
//                    float3 queryPos = new float3(tileX + dx + 0.5f, pos.y, tileZ + dz + 0.5f);
//                    if (!_mapQuery.TryGetTileData(queryPos, out MapTileData tile)) continue;

//                    float2 localCenter = playerXZ - new float2(tileX + dx, tileZ + dz);

//                    for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
//                    {
//                        if (!MapNaviTileUtil.IsSubTileValid(tile.NaviMask, s) &&
//                             MapNaviTileUtil.IsCircleOverlappingSubTile(s, localCenter, radiusSq))
//                        {
//                            return false;
//                        }
//                    }
//                }
//            }
//            return true;
//        }

//        private bool TrySampleHeight(Vector3 pos, out float groundY)
//        {
//            groundY = pos.y;
//            if (null == _mapQuery) return false;

//            float3 queryPos = new float3(pos.x, pos.y, pos.z);
//            if (!_mapQuery.TryGetTileData(queryPos, out MapTileData tile)) return false;

//            MapCoordUtil.ComputeKey(queryPos, out int gKey, out int tKey);
//            MapCoordUtil.GetPivot(gKey, tKey, out float3 tilePivot);

//            float tileBaseY = tilePivot.y;
//            float2 localPos = new float2(pos.x - tilePivot.x, pos.z - tilePivot.z);

//            for (int s = 0; s < MapConsts.TRIANGLES_COUNT; s++)
//            {
//                if (!MapNaviTileValid(tile.NaviMask, s)) continue;

//                int v0 = MapConsts.SubTileVertexMap[s * 3 + 0];
//                int v1 = MapConsts.SubTileVertexMap[s * 3 + 1];
//                int v2 = MapConsts.SubTileVertexMap[s * 3 + 2];

//                float2 p0 = MapConsts.VertexPositions[v0];
//                float2 p1 = MapConsts.VertexPositions[v1];
//                float2 p2 = MapConsts.VertexPositions[v2];

//                if (!MapGeometryUtil.IsPointInTriangle(localPos, p0, p1, p2)) continue;

//                int h0 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v0);
//                int h1 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v1);
//                int h2 = MapNaviTileUtil.GetHeightFromNaviMask(tile.NaviMask, v2);

//                float3 bary = MapGeometryUtil.BarycentricCoords(localPos, p0, p1, p2);
//                float sampledHeight = (bary.x * h0 + bary.y * h1 + bary.z * h2) * HEIGHT_STEP;

//                groundY = tileBaseY + sampledHeight;
//                return true;
//            }
//            return false;
//        }

//        private bool MapNaviTileValid(long naviMask, int s)
//        {
//            return MapNaviTileUtil.IsSubTileValid(naviMask, s);
//        }
//    }
//}