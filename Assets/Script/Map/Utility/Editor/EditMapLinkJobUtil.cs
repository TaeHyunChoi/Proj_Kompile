#if UNITY_EDITOR
namespace Script.Map.Utility
{
    using Script.Map.Data;
    using static Script.Map.Data.MapConsts;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    /// <summary> 주변 타일과의 연결 여부를 확인 및 저장 </summary>
    [BurstCompile]
    public struct EditMapLinkJobUtil : IJobParallelFor
    {
        [ReadOnly] public NativeArray<long> KeyArray;
        [ReadOnly] public NativeHashMap<long, EditMapTileData> Map;

        [WriteOnly] public NativeArray<EditMapTileData> Results;

        [ReadOnly] public NativeArray<float2> LinkDirs;
        [ReadOnly] public NativeArray<float> DiffYs;

        public void Execute(int index)
        {
            long myID = KeyArray[index];
            if (false == Map.TryGetValue(myID, out EditMapTileData myTile))
            {
                return;
            }

            int accumulatedLinkMask = 0;
            MapCoordUtil.ComputeWorldPosition(myID, out float3 myPivot);

            for (int i = 0; i < LinkDirs.Length; ++i)
            {
                float2 linkDir = LinkDirs[i];
                EditMapTileDirFlag dirFlag = MapNaviTileUtil.GetDirFlag(linkDir.x, linkDir.y);

                for (int y = 0; y < DiffYs.Length; ++y)
                {
                    float dy = DiffYs[y];
                    float3 targetDir = new float3(linkDir.x, dy, linkDir.y);
                    long neighborTileID = MapCoordUtil.ComputeTileID(myPivot + targetDir);
                    
                    if (true == CheckConnectable(neighborTileID, myTile, dirFlag))
                    {
                        const int LINK_ZERO = 0b_00;
                        const int LINK_UP   = 0b_01;
                        const int LINK_DOWN = 0b_10;
                        const int LINK_NONE = 0b_11;

                        int linkValue = dy switch
                        {
                            0  => LINK_ZERO,
                            1  => LINK_UP,
                            -1 => LINK_DOWN,
                            _  => LINK_NONE
                        };

                        accumulatedLinkMask |= (linkValue << MapNaviTileUtil.GetLinkMaskShift(dirFlag));
                        break;
                    }
                }
            }

            myTile.LinkMask = (ushort)accumulatedLinkMask;
            Results[index] = myTile;
        }

        private readonly bool CheckConnectable(long neighborTileID, EditMapTileData myTile, EditMapTileDirFlag dirFlag)
        {
            if (false == Map.TryGetValue(neighborTileID, out EditMapTileData neighborTile))
            {
                return false;
            }
            // 직선 방향 체크
            if (true == IsSingleDirection(dirFlag))
            {
                return IsSingleLinked(dirFlag, myTile, neighborTile);
            }

            // 대각선 체크 (Chain Link)
            EditMapTileDirFlag first = dirFlag & (EditMapTileDirFlag.LEFT | EditMapTileDirFlag.RIGHT);
            EditMapTileDirFlag second = dirFlag & (EditMapTileDirFlag.UP | EditMapTileDirFlag.DOWN);

            return IsChainLinked(myTile, neighborTile, first, second) &&
                   IsChainLinked(myTile, neighborTile, second, first);
        }
        private readonly bool IsSingleDirection(EditMapTileDirFlag dirFlag)
        {
            int count = 0;

            if (0 != (dirFlag & EditMapTileDirFlag.LEFT))  { count += 1; }
            if (0 != (dirFlag & EditMapTileDirFlag.RIGHT)) { count += 1; }
            if (0 != (dirFlag & EditMapTileDirFlag.UP))    { count += 1; }
            if (0 != (dirFlag & EditMapTileDirFlag.DOWN))  { count += 1; }

            return count == 1;
        }
        private readonly bool IsSingleLinked(EditMapTileDirFlag direction, EditMapTileData myTile, EditMapTileData neighborTile)
        {
            EditVertexContextData myV = MapNaviTileUtil.GetVertexIndexInfo(direction);

            var neighborDir = direction switch
            {
                EditMapTileDirFlag.LEFT  => EditMapTileDirFlag.RIGHT,
                EditMapTileDirFlag.RIGHT => EditMapTileDirFlag.LEFT,
                EditMapTileDirFlag.UP    => EditMapTileDirFlag.DOWN,
                EditMapTileDirFlag.DOWN  => EditMapTileDirFlag.UP,
                _ => throw new System.ArgumentException()
            };
            EditVertexContextData neighborV = MapNaviTileUtil.GetVertexIndexInfo(neighborDir);
            
            // 중앙 비교 (.center): 하나라도 vertice가 없으면 무조건 연결 불가;
            if (false == CompareVerticeHeight(myTile, myV.center, neighborTile, neighborV.center))
            {
                return false;
            }
            
            // 앙옆(.side0, .side1) 중 하나라도 동일하면 이어졌다고 판정
            if (true == CompareVerticeHeight(myTile, myV.side0, neighborTile, neighborV.side0)
                || true == CompareVerticeHeight(myTile, myV.side1, neighborTile, neighborV.side1))
            {
                return true;
            }
            
            return false;
        }
        private readonly bool IsChainLinked(EditMapTileData startTile, EditMapTileData targetTile, EditMapTileDirFlag first, EditMapTileDirFlag second)
        {
            MapCoordUtil.ComputeWorldPosition(startTile.ID, out float3 startPivot);
            float3 midPivot = startPivot + MapNaviTileUtil.GetDirectionVector(first);

            long midID = MapCoordUtil.ComputeTileID(midPivot);
            if (false == Map.TryGetValue(midID, out EditMapTileData midTile))
            {
                return false;
            }

            return IsSingleLinked(first, startTile, midTile)
                   && IsSingleLinked(second, midTile, targetTile);
        }
        
        private readonly bool CompareVerticeHeight(EditMapTileData my, int myV, EditMapTileData neighbor, int neighborV)
        {
            if (false == my.TryGetHeightMask(myV, out int myHMask)
                || false == neighbor.TryGetHeightMask(neighborV, out int neighborHMask))
            {
                return false;
            }

            return myHMask == neighborHMask;
        }
    }
}
#endif