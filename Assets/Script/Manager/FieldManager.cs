namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using Script.Util;
    using System.Threading.Tasks;
    using Unity.Mathematics;
    using UnityEngine;

    public class FieldManager
    {
        private static ConcurrentDictionary<int, MapGridData> currentMapGrid; // 일단 하나만 올려보자.
        private static IngameFieldPlayer[] player_character = new IngameFieldPlayer[3];

        private static IngameMapTileData[] target_tiles;

        public async Task<bool> Initialize(PlayData playData)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[FieldManager] Initialize(PlayerData)");
#endif

            // instantiage map
            currentMapGrid = new ConcurrentDictionary<int, MapGridData>();


#if UNITY_EDITOR
            // test: grid_0
            MapGridData grid = await AssetManager.InstaniateMapGrid(0);
            currentMapGrid.TryAdd(grid.gridKey, grid);
            
            // test: gird_8320
            grid = await AssetManager.InstaniateMapGrid(8320);
            currentMapGrid.TryAdd(grid.gridKey, grid);
#endif

            // instantiage player unit
            GameObject obj = await AssetManager.GetOrNewInstanceAsync(AssetCode.UnitBase, AssetParentType.UNIT_ROOT);

            // TODO: 테스트 목적이라서 나중에 다시 만들어야 함.
            player_character[0] = obj.AddComponent<IngameFieldPlayer>();
            IngameFieldPlayer player = player_character[0];

            target_tiles = new IngameMapTileData[4];

            if (true == await player.Init(0))
            {
                player.transform.position = new Vector3(1.5f, -1f, 1f);
                IngameManager.InitFollowingCamera(player);
            }
            else
            {
                UnityEngine.Debug.Assert(false, "[TEST] Fail to initialize player_character");
                return false;
            }

            MessageManager.Publish(new OnEndEvent(IngameEventType.FIELD_INIT));
            return true;
        }

        public static bool TryPlayerMove(float3 target_position, out float y)
        {
            y = target_position.y;

            // 충돌을 확인할 주변 타일 탐색
            if (false == TryGetAdjacentTiles(target_position, ref target_tiles)) 
            {
                return false;
            }

            float radius = 0.5f - float.Epsilon;
            bool isMovable = MapTileOverlapJobManager.Instance.CheckMapTileMovable(target_position, false, radius, target_tiles);
            if (false == isMovable)
            {
                return false;
            }

            // 다음 위치의 타일 정보 : IngameMapTileData target_tiles[0]; => GetTargetTiles(Vector3)에서 그렇게 정했음~!!
            IngameMapTileData targetTile = target_tiles[0];

            // 현재 위치가 i번 삼각형 안에 있다
            int i = MapUtil.GetTriangleIndex(target_position, false);

            // 삼각형 꼭지점 좌표 구하고..
            MapUtil.TryGetTrianglePoint(targetTile, i, 0, out float3 a);
            MapUtil.TryGetTrianglePoint(targetTile, i, 1, out float3 b);
            MapUtil.TryGetTrianglePoint(targetTile, i, 2, out float3 c);

            y = target_tiles[0].TilePosition.y + MapUtil.CalculateYOnPlane(a, b, c, target_position.x, target_position.z);
            return isMovable;
        }

        /// <summary> </summary>
        /// <returns>체크할 타일 개수</returns>
        private static bool TryGetAdjacentTiles(Vector3 target_position, ref IngameMapTileData[] targets)
        {
            // 다음 이동할 목표 좌표에 대하여 타일이 존재? 없으면 탐색 종료
            if (false == TryGetMapTileData(target_position, out MapTileData candidate_tile))
            {
                return false;
            }

            int index = 0;
            int grid_key = MapUtil.GetGridKeyMask(target_position);
            int tile_key = MapUtil.GetTileKeyMask(target_position);
            targets[index++] = new IngameMapTileData(grid_key, tile_key, candidate_tile);
            Debug.Log($"Target[{index - 1}], pos:{targets[index - 1].TilePosition}");

            // next_target_position을 기준으로 이웃한 타일이 어디인지 확인
            Vector3 target_pivot = MapUtil.GetTilePivotPosition(target_position, false);
            int quarant = MapUtil.GetQuarantInTile(target_position, false);
            int3 target_link = MapTileIndex.TILE_LINK_INDEX_BY_QUARANT[quarant];

            for (int i = 0; i < 3; ++i)
            {
                int link_mask = 0;
                switch (i)
                {
                    case 0:
                        link_mask = (candidate_tile.LinkMask >> (target_link.x * 2)) & 0b11;
                        break;
                    case 1:
                        link_mask = (candidate_tile.LinkMask >> (target_link.y * 2)) & 0b11;
                        break;
                    case 2:
                        link_mask = (candidate_tile.LinkMask >> (target_link.z * 2)) & 0b11;
                        break;
                    default:
                        break;
                };

                Vector3 neighbor_tile_pivot = target_pivot + MapTileIndex.RELATIVE_COORD_BY_QUARANT[quarant * 3 + i];
                float y;
                switch (link_mask)
                {
                    case MapTileIndex.ADJACENT_ZERO: y =  0f; break;
                    case MapTileIndex.ADJACENT_UP:   y =  1f; break;
                    case MapTileIndex.ADJACENT_DOWN: y = -1f; break;
                    default: //adjacent == null

                        grid_key = MapUtil.GetGridKeyMask(neighbor_tile_pivot);
                        tile_key = MapUtil.GetTileKeyMask(neighbor_tile_pivot);
                        targets[index++] = new IngameMapTileData(grid_key, tile_key, new MapTileData(MapTileIndex.NAVI_NULL));
                        Debug.Log($"ADJ[{index - 1}].IsValid? ({targets[index - 1].IsValid()}), pos:{targets[index - 1].TilePosition}");
                        continue;
                }

                neighbor_tile_pivot += y * Vector3.up;
                grid_key = MapUtil.GetGridKeyMask(neighbor_tile_pivot);
                tile_key = MapUtil.GetTileKeyMask(neighbor_tile_pivot);

                if (true == TryGetMapTileData(neighbor_tile_pivot, out candidate_tile))
                {
                    targets[index++] = new IngameMapTileData(grid_key, tile_key, candidate_tile);
                }
                else
                {
                    targets[index++] = new IngameMapTileData(grid_key, tile_key, new MapTileData(MapTileIndex.NAVI_NULL));
                }

                Debug.Log($"ADJ[{index - 1}].IsValid? ({targets[index - 1].IsValid()}) pos:{targets[index - 1].TilePosition}");
                //// 해당 타일의 데이터가 없는데 좌표는 겹친다 => 이동 불가하도록 처리 (ex. 맵 끝에 도달)
                //else if (true == MapUtil.IsOverlaped(neighbor_tile_pivot, 1f, target_position))
                //{
                //    continue;
                //}
            }

            return true;
        }

        public static bool TryGetMapTileData(float3 position, out MapTileData tile)
        {
            int gKey = MapUtil.GetGridKeyMask(position);
            if (false == currentMapGrid.ContainsKey(gKey))
            {
                tile = default;
                return false;
            }

            int tKey = MapUtil.GetTileKeyMask(position);

            return currentMapGrid[gKey].TryGetTileData(tKey, out tile);
        }

        ~FieldManager()
        {
            foreach (var grid in currentMapGrid.Values)
            {
                grid.Dispose();
            }

            player_character = null;
        }
    }
}