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

        private static readonly Stopwatch stopwatch = new Stopwatch();

        public static bool TryPlayerMove(float3 target_position, out float y)
        {
            y = target_position.y;

            // 충돌을 확인할 주변 타일 탐색
            if (0 >= SearchNeighborTiles(target_position)) 
            {
                return false;
            }

            float radius = 0.5f;
            bool isMovable = MapTileOverlapJobManager.Instance.CheckMapTileMovable(target_position, isSmall, radius, target_tiles);
            if (false == isMovable)
            {
                return false;
            }

            // 다음 위치의 타일 정보 : IngameMapTileData target_tiles[0]; => GetTargetTiles(Vector3)에서 그렇게 정했음~!!
            IngameMapTileData targetTile = target_tiles[0];

            // 현재 위치가 i번 삼각형 안에 있다
            int i = MapUtil.GetTriangleIndex(target_position, isSmall);

            // 삼각형 꼭지점 좌표 구하고..
            MapUtil.TryGetTrianglePoint(targetTile, i, 0, out float3 a);
            MapUtil.TryGetTrianglePoint(targetTile, i, 1, out float3 b);
            MapUtil.TryGetTrianglePoint(targetTile, i, 2, out float3 c);

            y = MapUtil.CalculateYOnPlane(a, b, c, target_position.x, target_position.z);

            // 6. Job 실행 시간 측정 종료
            //stopwatch.Stop();
            //UnityEngine.Debug.Log($"[TEST] {a:F3},{b:F3},{c:F3} => ({next_position.x:F3}, {y:F3},{next_position.z:F3}), {stopwatch.Elapsed.TotalMilliseconds:F3} ms");

            return isMovable;
        }

        /// <summary> </summary>
        /// <returns>체크할 타일 개수</returns>
        private static int SearchNeighborTiles(Vector3 target_position)
        {
            // 생각할수록 이상하네.. 게임 연산은 되도록 tile을 쓰는게 좋지 않나?

            // 다음 이동할 목표 좌표에 대하여 타일값이 유효하게 존재하는지 확이
            // 만약 존재하지 않는다면 탐색 종료
            if (false == TryGetMapTileData(target_position, out MapTileData mapTileData))
            {
                return 0;
            }

            int grid_key = MapUtil.GetGridKeyMask(target_position);
            int tile_key = MapUtil.GetTileKeyMask(target_position);
            int index = 0;
            int target_link_mask = mapTileData.LinkMask;

            target_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);


            // next_target_position을 기준으로 이웃한 타일이 어디인지 확인
            int quarant = MapUtil.GetQuarantInTile(target_position, isSmall);
            Vector3 tPivot = MapUtil.GetTilePivotPosition(target_position, isSmall);
            Vector3 neighbor_tile_pivot;




            //for (int i = 0; i < 3; ++i)
            //{
            //    neighbor_tile_pivot = tPivot + TileScale * MapTileIndex.RELATIVE_COORD_BY_QUARANT[quarant * 3 + i];
            //    if (true == MapUtil.TryGetNeighborLinkValue(quarant, i, target_link_mask, out int y))
            //    {
            //        neighbor_tile_pivot += y * Vector3.up;
            //    }


            //    if (true == TryGetMapTileData(neighbor_tile_pivot, out mapTileData))
            //    {
            //        target_tiles[index++] = new IngameMapTileData(grid_key, tile_key, mapTileData);
            //    }
            //    // 해당 타일의 데이터가 없는데 좌표는 겹친다 => 이동 불가하도록 처리 (ex. 맵 끝에 도달)
            //    else if(true == MapUtil.IsOverlaped(neighbor_tile_pivot, TileScale, target_position))
            //    {
            //        return 0;
            //    }
            //}

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