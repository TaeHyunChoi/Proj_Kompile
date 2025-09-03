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
            UnityEngine.Debug.Log($"[FieldManager] Initialize(PlayerData)");
#endif

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
                UnityEngine.Debug.Assert(false, "[TEST] Fail to initialize player_character");
                return false;
            }

            MessageManager.Publish(new OnEndEvent(IngameEventType.FIELD_INIT));
            return true;
        }

        public static bool TryMovePlayer(Vector3 next_position, out float y)
        {
            // 3. Job 실행 시간 측정 시작
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            y = 0f;

            int count = GetTargetTiles(next_position);
            if (0 >= count)
            {
                return false;
            }

            float radius = 1f;

            // 근데 앞에서 isMovable;을 확인했으므로 사실 필요 없긴 함 ㅎ; 
            bool isMovable = MapTileOverlapJobManager.Instance.CheckMapTileMovable(next_position, isSmall, radius, target_tiles);
            if (false == isMovable)
            {
                return false;
            }

            // 다음 위치의 타일 정보 : IngameMapTileData target_tiles[0]; => GetTargetTiles(Vector3)에서 그렇게 정했음~!!
            IngameMapTileData targetTile = target_tiles[0];

            // 현재 위치가 i번 삼각형 안에 있다
            int i = MapUtil.GetTriangleIndex(next_position, isSmall);

            // 삼각형 꼭지점 좌표 구하고..
            MapUtil.TryGetTrianglePoint(targetTile, i, 0, out float3 a);
            MapUtil.TryGetTrianglePoint(targetTile, i, 1, out float3 b);
            MapUtil.TryGetTrianglePoint(targetTile, i, 2, out float3 c);

            y = MapUtil.CalculateYOnPlane(a, b, c, next_position.x, next_position.z);

            // 6. Job 실행 시간 측정 종료
            stopwatch.Stop();
            UnityEngine.Debug.Log($"[TEST] {a:F1},{b:F1},{c:F1} => ({next_position.x:F1}, {y:F1},{next_position.z:F1}), {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

            return isMovable; 
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
                // 해당 타일의 데이터가 없는데 좌표는 겹친다 => 이동 불가하도록 처리 (ex. 맵 끝에 도달)
                else if(true == MapUtil.IsOverlaped(neighbor_tile_pivot, TileScale, next_move_position))
                {
                    return 0;
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
        }
    }
}