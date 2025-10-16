namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using Script.Util;
    using System.Threading.Tasks;
    using Unity.Mathematics;
    using UnityEngine;
    using System.Diagnostics;

    public class FieldManager
    {
        private static ConcurrentDictionary<int, MapGridData> currentMapGrid; // 일단 하나만 올려보자.
        private static IngameFieldPlayer[] player_character = new IngameFieldPlayer[3];

        private static IngameMapTileData[] target_tiles;

        private static bool isSmall;
        private static float TileScale
        {
            get
            {
                return (true == isSmall) ? 0.5f : 1f;
            }
        }

        public async Task<bool> Initialize(PlayData playData)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[FieldManager] Initialize(PlayerData)");
#endif

            // instantiage map
            currentMapGrid = new ConcurrentDictionary<int, MapGridData>();


#if UNITY_EDITOR
            MapGridData grid;
            int[] test_grid = new int[] { 0, 65, 8385 };

            for (int i = 0; i < test_grid.Length; ++i)
            {
                grid = await AssetManager.InstaniateMapGrid(test_grid[i]);
                currentMapGrid.TryAdd(grid.gridKey, grid);
            }
#endif

            // instantiage player unit
            GameObject obj = await AssetManager.GetOrNewInstanceAsync(AssetCode.UnitBase, AssetParentType.UNIT_ROOT);

            // TODO: 테스트 목적이라서 나중에 다시 만들어야 함.
            player_character[0] = obj.AddComponent<IngameFieldPlayer>();
            IngameFieldPlayer player = player_character[0];

            target_tiles = new IngameMapTileData[4];

            if (true == await player.Init(0))
            {
                player.transform.position = new Vector3(1f, -1f, 0.5f);
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

            if (false == TryGetLinkedTiles(target_position))
            {
                //UnityEngine.Debug.LogError($"[MoveOnTile] Fail to TryGetLinkedTiles({target_position})");
                return false;
            }

            float radius = 0.3f;
            bool isMovable = MapTileOverlapJobManager.Instance.CheckMapTileMovable(target_position, isSmall, radius, target_tiles);
            if (false == isMovable)
            {
                //UnityEngine.Debug.LogError($"[MoveOnTile] Fail to CheckMapTileMovable({target_position}, false, {radius}, ...)");
                return false;
            }

            // 다음 위치의 타일 정보 : IngameMapTileData target_tiles[0]; => GetTargetTiles(Vector3)에서 그렇게 정했음~!!
            IngameMapTileData targetTile = target_tiles[0];

            int i = MapUtil.GetTriangleIndex(target_position, false);

            // 삼각형 꼭지점 좌표 구하고..
            MapUtil.TryGetTrianglePoint(targetTile, i, 0, true, out float3 a);
            MapUtil.TryGetTrianglePoint(targetTile, i, 1, true, out float3 b);
            MapUtil.TryGetTrianglePoint(targetTile, i, 2, true, out float3 c);

            y = MapUtil.CalculateYOnPlane(a, b, c, target_position.x, target_position.z);

            UnityEngine.Debug.Log($"[MoveOnTile] {target_position} => {targetTile.TilePosition}({i})\n => {a},{b},{c} => {y}");

            return isMovable;
        }

        private static bool TryGetLinkedTiles(Vector3 target_position)
        {
            // 다음 이동할 목표 좌표에 대하여 타일값이 유효하게 존재하는가?
            if (false == TryGetMapTileData(target_position, out MapTileData mapTileData))
            {
                return false;
            }

            int grid_key = MapUtil.GetGridKeyMask(target_position);
            int tile_key = MapUtil.GetTileKeyMask(target_position);
            int index = 0;
            int target_link_mask = mapTileData.LinkMask;

            // 현재 위치한 타일++
            target_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);

            // next_target_position을 기준으로 이웃한 타일이 어디인지 확인
            int quarant = MapUtil.GetQuarantInTile(target_position, isSmall);
            var link = quarant switch
            {
                0 => new int3(3, 4, 5),
                1 => new int3(5, 6, 7),
                2 => new int3(7, 0, 1),
                _ => new int3(1, 2, 3),
            };
            Vector3 tPivot = MapUtil.GetTilePivotPosition(target_position, isSmall);
            Vector3 neighbor_tile_pivot;

            for (int i = 0; i < 3; ++i)
            {
                int q = i switch
                {
                    0 => link.x,
                    1 => link.y,
                    _ => link.z
                };

                neighbor_tile_pivot = tPivot + TileScale * MapTileIndex.RELATIVE_COORD_BY_QUARANT[q];
                grid_key = MapUtil.GetGridKeyMask(neighbor_tile_pivot);
                tile_key = MapUtil.GetTileKeyMask(neighbor_tile_pivot);

                // 연결 여부 확인
                if (true == MapUtil.TryGetLinkValue(target_link_mask, q, out int y))
                {
                    neighbor_tile_pivot += y * Vector3.up;
                }
                else
                {
                    //연결 여부가 없다면? none_data 입력
                    target_tiles[index++] = new IngameMapTileData(grid_key, tile_key);
                    continue;
                }

                // 타일 존재 확인
                if (true == TryGetMapTileData(neighbor_tile_pivot, out mapTileData))
                {
                    target_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);
                }
                else
                {
                    // none_data
                    target_tiles[index++] = new IngameMapTileData(grid_key, tile_key);
                }
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