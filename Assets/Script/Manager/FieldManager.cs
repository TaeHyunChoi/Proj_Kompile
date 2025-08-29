namespace Script.Manager
{
    using Script.Data;
    using Script.Index;
    using Script.IngameMessage;
    using Script.Util;
    using static Script.Index.MapTileIndex;
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

        public static void CheckMove(Vector3 next_position)
        {
            if (0 >= GetTargetTiles(next_position))
            {
                return;
            }
            // 체크할 타일이 유효한지 확인 필요
            // 테스트 목적으로 구조체도 하나 만들고..
        }

        /// <summary> </summary>
        /// <param name="quarant"></param>
        /// <returns>체크할 타일 개수</returns>
        private static int GetTargetTiles(Vector3 position)
        {
            int gKey, tKey;

            // 본인 position 먼저 체크하고...


            //if (false == IsTileValid(position))
            //{
            //    return 0;
            //}
            // 이후에 주변부 체크한다.

            // 계속 거슬리는 포인트: coord_key, coord_position
            // IngameTileMap에 담긴 정보들이 필요한 것인데..
            // 변환할 게 너무 많고 번거롭다!라는 인상...



            //Vector3 tile_pivot  = MapUtil.GetTilePivot(position);
            //int quarant         = MapUtil.GetQuarantInTile(tile_pivot, position);

            return 0;
        }




        public static bool TryCheckOverlapTiles(int grid_key, Vector3 next_position, out IngameMapTileData[] target_tiles)
        {
            //매번 새롭게 배열 생성할 필요가 없음 -> 어드메에 static으로 넘겨두면 되지 않나요?
            target_tiles = new IngameMapTileData[4];

            // 현재 위치가 속한 타일의 pivot_position을 기준으로 가자.
            // grid_key도 나중에 잡아가야 하는거네.

            // tile pivot position
            //next_position 에서 TilePivotPosition을 구하려면 어떻게 해야 하니?
            //Vector3 tile_pivot = MapUtil.GetTilePivot(next_position);
            // 잠깐만...


            return true;
            //// 다시 생각해보자.
            //int index = 0;
            //int isSmallFlag;

            //// next_position 기준으로 본인 타일 먼저 확인
            //// "하나라도 타일 정보가 없으면 다음 위치로 이동 불가하다."
            //int next_tile_key = MapUtil.GetTileCoordKey(next_position);
            //if (true == currentMapGrid[grid_key].TryGetTileData(next_tile_key, out MapTileData tile))
            //{
            //    isSmallFlag = 0;
            //}
            //else if (true == currentMapGrid[grid_key].TryGetTileData(next_tile_key | 1 << SHIFT_TILE_SMALL, out tile))
            //{
            //    isSmallFlag = 1 << SHIFT_TILE_SMALL;
            //}
            //else
            //{
            //    return false;
            //}
            //target_tiles[index++] = new IngameMapTileData(grid_key, next_tile_key, tile);

            //// next_position이 현재 타일의 몇사분면 위에 있는지 확인 -> 인접 타일 coord_key 구하기
            //Vector3 next_tile_pivot = MapUtil.GetTilePivot(grid_key, next_tile_key);
            //int quarant = MapUtil.GetQuarantInTile(next_tile_pivot, next_position);

            //// 코드 중복 추후에 수정 요망
            //MapTileData neighbor;
            //int tile_coord_mask;

            //var x1Mask = 1 << SHIFT_TILE_X;
            //var z1Maxk = 1 << SHIFT_TILE_Z;

            // ----------------

            //switch (quarant)
            //{
            //    case 1: // (x+1), (z+1), (x+1, z+1);
            //        // 여기서부터 grid_key가 달라질 수도 있는거 아니오?
            //        // 그냥 Vector3 position 구해서 grid, tile 구하는게 나을 것 같은데?
            //        tile_coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = new IngameMapTileData();
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_Z);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X + 1 << MapUtil.SHIFT_TILE_Z);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }
            //        break;
            //    case 2: // (x-1), (z+1), (x-1, z+1)
            //        tile_coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_X);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_Z);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_X + 1 << MapUtil.SHIFT_TILE_Z);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }
            //        break;
            //    case 3: // (x-1), (z-1), (x-1, z-1)
            //        tile_coord_mask = isSmallFlag | (next_tile_key - x1Mask);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key - z1Maxk);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key - x1Mask - z1Maxk);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }
            //        break;
            //    case 4: // (x+1), (z-1), (x+1, z-1)
            //        tile_coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key - 1 << MapUtil.SHIFT_TILE_Z);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }

            //        tile_coord_mask = isSmallFlag | (next_tile_key + 1 << MapUtil.SHIFT_TILE_X - 1 << MapUtil.SHIFT_TILE_Z);
            //        if (true == currentMapGrid[grid_key].TryGetTileData(tile_coord_mask, out neighbor))
            //        {
            //            target_tiles[index++] = neighbor;
            //        }
            //        else
            //        {
            //            return false;
            //        }
            //        break;
            //    default:
            //        return false;
            //}

            //return index > 0;
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