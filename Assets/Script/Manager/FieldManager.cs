namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using Script.Util;
    using System.Threading.Tasks;
    using UnityEngine;
    
    public class FieldManager
    {
        private static FieldManager instance;
        public static FieldManager Instance => instance;

        private static ConcurrentDictionary<int, MapGridData> currentMapGrid; // 일단 하나만 올려보자.
        private static IngameFieldPlayer[] player_character = new IngameFieldPlayer[3];

        private static IngameMapTileData[] target_tiles;
        //public static int SceneIndex => currentMapGrid[0].GetSceneIndex();
        //public static MapGridData MapGrid => currentMapGrid[0];

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

            target_tiles = new IngameMapTileData[4];

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

        public static void CheckPlayerMove(Vector3 next_position)
        {
            int count = GetTargetTiles(next_position);
            if (0 >= count)
            {
                return;
            }

            float radius = 0.5f;
            MapTileOverlapJobManager.Instance.ScheduleJob_MapTileMovable(next_position, isSmall, radius, target_tiles);
        }

        /// <summary> </summary>
        /// <returns>체크할 타일 개수</returns>
        private static int GetTargetTiles(Vector3 next_move_position)
        {
            // 다음 이동할 목표 좌표에 대하여 타일값이 유효하게 존재하는지 확이
            // 만약 존재하지 않는다면 탐색 종료
            (int grid_key, int tile_key) = MapUtil.GetCoordKey(next_move_position, false);
            if (false == TryGetMapTileData(grid_key, tile_key, out MapTileData mapTileData))
            {
                return 0;
            }

            int index = 0;
            target_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);

            // next_target_position을 기준으로 이웃한 타일이 어디인지 확인
            int quarant = MapUtil.GetQuarantInTile(next_move_position, isSmall);
            Vector3 tPivot = MapUtil.GetTilePivotPosition(next_move_position, isSmall);
            Vector3 neighbor_tile_pivot;

            for (int i = 0; i < 3; ++i)
            {
                neighbor_tile_pivot = tPivot + TileScale * MapTileIndex.RELATIVE_COORD_BY_QUARANT[quarant * 3 + i];
                (grid_key, tile_key) = MapUtil.GetCoordKey(neighbor_tile_pivot, false);

                if (true == TryGetMapTileData(grid_key, tile_key, out mapTileData))
                {
                    target_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);
                }
            }

            return index;
        }

        private static bool TryGetMapTileData(int gKey, int tKey, out MapTileData mapTileData)
        {
            if (false == currentMapGrid.ContainsKey(gKey))
            {
                mapTileData = default;
                return false;
            }

            return currentMapGrid[gKey].TryGetTileData(tKey, out mapTileData);
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