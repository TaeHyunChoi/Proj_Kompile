using static EditMapConsts;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary> 주변 타일과의 연결 여부(단차)를 확인 및 비트마스킹으로 저장합니다. </summary>
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

        // [수정 1] 0(LINK_ZERO)이 아닌 0xFFFF(모든 방향 LINK_NONE)으로 초기화합니다.
        int accumulatedLinkMask = 0xFFFF;
        EditMapCoordUtil.ComputeWorldPosition(myID, out float3 myPivot);

        for (int i = 0; i < LinkDirs.Length; ++i)
        {
            float2 linkDir = LinkDirs[i];
            EditMapTileDirFlag dirFlag = EditMapNaviTileUtil.GetDirFlag(linkDir.x, linkDir.y);
            int shift = EditMapNaviTileUtil.GetLinkMaskShift(dirFlag);

            for (int y = 0; y < DiffYs.Length; ++y)
            {
                float dy = DiffYs[y];
                float3 targetDir = new float3(linkDir.x, dy, linkDir.y);
                long neighborTileID = EditMapCoordUtil.ComputeTileID(myPivot + targetDir);

                if (true == CheckConnectable(neighborTileID, myTile, dirFlag))
                {
                    const int LINK_ZERO = 0b_00;
                    const int LINK_UP = 0b_01;
                    const int LINK_DOWN = 0b_10;
                    // const int LINK_NONE = 0b_11; (초기값으로 이미 세팅됨)

                    int linkValue = dy switch
                    {
                        0 => LINK_ZERO,
                        1 => LINK_UP,
                        -1 => LINK_DOWN,
                        _ => 0b_11
                    };

                    // [수정 2] 해당 방향의 2비트를 초기화(00)한 뒤, 연결된 단차 값을 주입합니다.
                    accumulatedLinkMask &= ~(0b_11 << shift);
                    accumulatedLinkMask |= (linkValue << shift);
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

        if (true == IsSingleDirection(dirFlag))
        {
            return IsSingleLinked(dirFlag, myTile, neighborTile);
        }

        EditMapTileDirFlag first = dirFlag & (EditMapTileDirFlag.LEFT | EditMapTileDirFlag.RIGHT);
        EditMapTileDirFlag second = dirFlag & (EditMapTileDirFlag.UP | EditMapTileDirFlag.DOWN);

        return IsChainLinked(myTile, neighborTile, first, second) &&
               IsChainLinked(myTile, neighborTile, second, first);
    }

    private readonly bool IsSingleDirection(EditMapTileDirFlag dirFlag)
    {
        int count = 0;
        if (0 != (dirFlag & EditMapTileDirFlag.LEFT)) { count += 1; }

        if (0 != (dirFlag & EditMapTileDirFlag.RIGHT)) { count += 1; }

        if (0 != (dirFlag & EditMapTileDirFlag.UP)) { count += 1; }

        if (0 != (dirFlag & EditMapTileDirFlag.DOWN)) { count += 1; }

        return count == 1;
    }

    private readonly bool IsSingleLinked(EditMapTileDirFlag direction, EditMapTileData myTile,
        EditMapTileData neighborTile)
    {
        EditVertexContextData myV = EditMapNaviTileUtil.GetVertexIndexInfo(direction);

        var neighborDir = direction switch
        {
            EditMapTileDirFlag.LEFT => EditMapTileDirFlag.RIGHT,
            EditMapTileDirFlag.RIGHT => EditMapTileDirFlag.LEFT,
            EditMapTileDirFlag.UP => EditMapTileDirFlag.DOWN,
            EditMapTileDirFlag.DOWN => EditMapTileDirFlag.UP,
            _ => throw new System.ArgumentException()
        };
        EditVertexContextData neighborV = EditMapNaviTileUtil.GetVertexIndexInfo(neighborDir);

        if (false == CompareVerticeHeight(myTile, myV.center, neighborTile, neighborV.center))
        {
            return false;
        }

        if (true == CompareVerticeHeight(myTile, myV.side0, neighborTile, neighborV.side0)
            || true == CompareVerticeHeight(myTile, myV.side1, neighborTile, neighborV.side1))
        {
            return true;
        }

        return false;
    }

    private readonly bool IsChainLinked(EditMapTileData startTile, EditMapTileData targetTile, EditMapTileDirFlag first,
        EditMapTileDirFlag second)
    {
        EditMapCoordUtil.ComputeWorldPosition(startTile.ID, out float3 startPivot);
        float3 firstDir = EditMapNaviTileUtil.GetDirectionVector(first);

        // [수정 3] 대각선 연결 시 거쳐가는 중간 타일(midTile)이 다른 Y층에 있을 수 있으므로 모두 검사합니다.
        for (int y = 0; y < DiffYs.Length; ++y)
        {
            float3 midPivot = startPivot + firstDir;
            midPivot.y += DiffYs[y];

            long midID = EditMapCoordUtil.ComputeTileID(midPivot);
            if (Map.TryGetValue(midID, out EditMapTileData midTile))
            {
                if (IsSingleLinked(first, startTile, midTile) && IsSingleLinked(second, midTile, targetTile))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private readonly bool CompareVerticeHeight(EditMapTileData my, int myV, EditMapTileData neighbor, int neighborV)
    {
        if (false == my.TryGetHeightMask(myV, out int myHMask)
            || false == neighbor.TryGetHeightMask(neighborV, out int neighborHMask))
        {
            return false;
        }

        // 15(0xF) 또는 -1은 삭제된 정점을 의미하므로 무조건 연결 불가 처리
        if (myHMask == 15 || neighborHMask == 15 || myHMask == -1 || neighborHMask == -1)
        {
            return false;
        }

        // [수정 4] 타일의 베이스 Y좌표를 불러와 절대 높이(Absolute Height)를 구하여 단차를 완벽히 매칭합니다.
        EditMapCoordUtil.ComputeWorldPositionInt(my.ID, out int3 myPos);
        EditMapCoordUtil.ComputeWorldPositionInt(neighbor.ID, out int3 nPos);

        // 1 층(Layer) 차이는 로컬 정점 높이 8칸(0.125 * 8 = 1.0)에 해당합니다.
        int myAbsoluteHeight = (myPos.y * 8) + myHMask;
        int neighborAbsoluteHeight = (nPos.y * 8) + neighborHMask;

        return myAbsoluteHeight == neighborAbsoluteHeight;
    }
}