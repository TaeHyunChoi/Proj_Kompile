namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using Script.Util;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public class FieldManager
    {
        private static FieldManager instance;
        public static FieldManager Instance => instance;

        private static ConcurrentDictionary<int, MapGridData> currentMapGrid; // 일단 하나만 올려보자.
        private static IngameFieldPlayer[] player_character = new IngameFieldPlayer[3];


        public static int SceneIndex => currentMapGrid[0].GetSceneIndex();
        public static MapGridData MapGrid => currentMapGrid[0];

        public async Task<bool> Initialize(PlayData playData)
        {
#if UNITY_EDITOR
            Debug.Log($"[FieldManager] Initialize(PlayerData)");
#endif
            if (null != instance)
            {
                return false;
            }
            instance = this;

            // instantiage map
            currentMapGrid = new ConcurrentDictionary<int, MapGridData>();
            var grid = await AssetManager.InstaniateMapGrid(playData.Grid);
            currentMapGrid.TryAdd(grid.gridKey, grid);

            // instantiage player unit
            GameObject obj = await AssetManager.GetOrNewInstanceAsync(AssetCode.UnitBase, AssetParentType.UNIT_ROOT);

            // TODO: 테스트 목적이라서 나중에 다시 만들어야 함.
            player_character[0] = obj.AddComponent<IngameFieldPlayer>();
            IngameFieldPlayer player = player_character[0];

            if (true == await player.Init(0))
            {
                player.transform.position = new Vector3(1, 0, 1);
                IngameManager.InitFollowingCamera(player);
            }
            else
            {
                Debug.Assert(false, "[TEST] Fail to initialize player_character");
                return false;
            }

            MessageManager.Publish(new OnEndEvent(IngameEventType.FIELD_INIT));
            return true;
        }

        public static bool ContainMapGrid(int grid_coord_key)
        {
            return currentMapGrid.ContainsKey(grid_coord_key);
        }

        public static bool TryGetCollisionTiles(int grid_key, Vector3 next_position, out MapTileData[] target_tiles)
        {
            target_tiles = new MapTileData[4]
            {
                new MapTileData(-1, 0),
                new MapTileData(-1, 0),
                new MapTileData(-1, 0),
                new MapTileData(-1, 0)
            };
            int index = 0;
            int isSmallFlag;

            // next_position 기준으로 본인 타일 먼저 확인
            int next_tile_key = MapUtil.GetTileCoordKey(next_position);
            if (true == currentMapGrid[grid_key].TryGetTileData(next_tile_key, out MapTileData tile))
            {
                isSmallFlag = 0;
            }
            else if (true == currentMapGrid[grid_key].TryGetTileData(next_tile_key | 1 << MapUtil.SHIFT_TILE_SMALL, out tile))
            {
                isSmallFlag = 1 << MapUtil.SHIFT_TILE_SMALL;
            }
            else
            {
                return false;
            }
            target_tiles[index++] = tile;

            // next_position이 현재 타일의 몇사분면 위에 있는지 확인 -> 인접 타일 coord_key 구하기
            Vector3 next_tile_pivot = MapUtil.GetTilePivot(grid_key, next_tile_key);
            int quarant = MapUtil.GetQuarantInTile(next_tile_pivot, next_position);

            // 코드 중복 추후에 수정 요망
            MapTileData neighbor;
            int coord_mask;
            switch (quarant)
            {
                case 1: // (x+1), (z+1), (x+1, z+1);
                    coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X + 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }
                    break;
                case 2: // (x-1), (z+1), (x-1, z+1)
                    coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_X);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_X + 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }
                    break;
                case 3: // (x-1), (z-1), (x-1, z-1)
                    coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_X);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_X - 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }
                    break;
                case 4: // (x+1), (z-1), (x+1, z-1)
                    coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }

                    coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X - 1 << MapUtil.SHIFT_TILE_Z);
                    if (true == currentMapGrid[grid_key].TryGetTileData(coord_mask, out neighbor))
                    {
                        target_tiles[index++] = neighbor;
                    }
                    break;
                default:
                    return false;
            }

            return true;
        }

        ~FieldManager()
        {
            foreach (var grid in currentMapGrid.Values)
            {
                grid.Dispose();
            }

            player_character = null;
            instance = null;
        }
    }
}