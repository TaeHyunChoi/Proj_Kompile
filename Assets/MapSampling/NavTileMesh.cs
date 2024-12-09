using System;
using UnityEngine;
using System.Threading.Tasks;
using Script.Util;
using DataStruct;

[Serializable]
public class NavTileMesh : MonoBehaviour
{
    [SerializeField] private ulong naviMask;
    [SerializeField] private uint  infoMask;
    
    public void InitNaviMask(int[] heights, bool isSmall)
    {
        Int32 i = 0;
        foreach (int height in heights)
        {
            ulong h;
            if (-1 == height)
            {
                h = 0b1111;
            }
            else
            {
                h = (ulong)height;
            }

            naviMask |= h << i;
            i += 4;
        }

        if (true == isSmall)
        {
            naviMask |= 1ul << i;
        }
    }
    public async Task  BakeMesh(ConcurrentDictionary<uint, ConcurrentDictionary<ulong, MapNavData>> map)
    {
        await Task.Yield();
        
        int rot = (transform.rotation.eulerAngles.y).ToInt();
        rot = (rot + 360) % 360;
        if (0 != rot % 90)
        {
            Debug.LogError($"Wrong Rotate: {rot}");
            return;
        }

        // calculate (rotated) pivot
        bool isSmall = (naviMask >> (4 * 13)) != 0;
        GetPivotRotated(rot, isSmall, out Vector3 gridPivot, out Vector3 tilePivot);
        
        // get key mask
        ushort gridKeyMask = GetGridKeyMask(gridPivot);
        map.TryAdd(gridKeyMask, new ConcurrentDictionary<ulong, MapNavData>());

        uint tileKeyMask = GetTileKeyMask(tilePivot - gridPivot, isSmall);
        naviMask = GetNaviMaskRotated(rot, isSmall); // ?? of 64 bits used
        infoMask = 0; // mesh index 넣어봅시다.
        map[gridKeyMask].TryAdd(tileKeyMask, new MapNavData(naviMask, infoMask));
    }
    
    
    // Key(pivot)
    private void GetPivotRotated(int rot, bool isSmall, out Vector3 gridPivot, out Vector3 tilePivot)
    {
        // tile pivot
        Vector3 rotated;
        switch (rot)
        {
            case  90: rotated = new Vector3( 0f, 0f, -1f); break;
            case 180: rotated = new Vector3(-1f, 0f, -1f); break;
            case 270: rotated = new Vector3(-1f, 0f,  0f); break;
            default:  rotated = Vector3.zero; break;
        }
        rotated *= isSmall ? 0.5f : 1f;
        tilePivot = transform.position + rotated;

        // grid pivot
        var gx = Mathf.FloorToInt(tilePivot.x / 32) * 32;
        var gy = Mathf.FloorToInt(tilePivot.y / 4)  *  4;
        var gz = Mathf.FloorToInt(tilePivot.z / 32) * 32;
        gridPivot = new Vector3(gx, gy, gz);
    }
    private ushort GetGridKeyMask(Vector3 gridPivot)
    {        
        const byte shiftGridXSign = 15;
        const byte shiftGridX     = 10;
        const byte shiftGridYSign =  9;
        const byte shiftGridY     =  6;
        const byte shiftGridZSign =  5;
        const byte shiftGridZ     =  0;
        
        Vector3Int gridInt = gridPivot.ToInt();
        
        int gridFlag = 0;

        if (gridInt.x < 0)
        {
            gridFlag |= 1 << shiftGridXSign;
            gridFlag |= (-gridInt.x) << shiftGridX;
        }
        else
        {
            gridFlag |= gridInt.x << shiftGridX;
        }

        if (gridInt.y < 0)
        {
            gridFlag |= 1 << shiftGridYSign;
            gridFlag |= (-gridInt.y) << shiftGridY;
        }
        else
        {
            gridFlag |= gridInt.y << shiftGridY;
        }

        if (gridInt.z < 0)
        {
            gridFlag |= 1 << shiftGridZSign;
            gridFlag |= (-gridInt.z) << shiftGridZ;
        }
        else
        {
            gridFlag |= gridInt.z << shiftGridZ;
        }

        return (ushort)gridFlag;
    }
    private uint GetTileKeyMask(Vector3 diff, bool isSmall)
    {
        int mask = 0;
        
        if (true == isSmall)
        {
            diff *= 2f;
        }
        Vector3Int diffInt = diff.ToInt();
        
        // scale ,x[sign,small_buffer,6], y[sign,small_buffer,4], z[sign,small_buffer,6]
        const byte shiftIsHalfScale = 22;
        const byte shiftTileX = 14;
        const byte shiftTileY = 8;
        const byte shiftTileZ = 0;
        
        mask |= isSmall ? 1 << shiftIsHalfScale : 0;
        mask |= (diffInt.x) << shiftTileX;
        mask |= (diffInt.y) << shiftTileY;
        mask |= (diffInt.z) << shiftTileZ;

        return (uint)mask;
    }


    private ushort GetTileIndexFlag(Vector3Int diffInt)
    {
        const byte shiftIsHalfScale = 15;
        const byte shiftTileX = 9;
        const byte shiftTileY = 6;
        const byte shiftTileZ = 0;

        var tileFlag = 0;
        // tileFlag |= isHalfScale ? 1 << shiftIsHalfScale : 0;
        tileFlag |= (diffInt.x) << shiftTileX;
        tileFlag |= (diffInt.y) << shiftTileY;
        tileFlag |= (diffInt.z) << shiftTileZ;

        return (ushort)tileFlag;
    }
    
    
    // Height
    private ulong GetNaviMaskRotated (int rot, bool isSmall)
    {
        ulong newMask = 0;
        
        var matrix = GetHeightMatrixRotated(rot);
        matrix = RotateMatrix(matrix, rot);

        ulong mask = 0;
        int i = 0;
        for (i = 0; i < 13; ++i)
        {
            switch (i)
            {
                case  0: mask = (ulong)matrix[0,4]; break;
                case  1: mask = (ulong)matrix[2,4]; break;
                case  2: mask = (ulong)matrix[4,4]; break;
                case  3: mask = (ulong)matrix[1,3]; break;
                case  4: mask = (ulong)matrix[3,3]; break;
                case  5: mask = (ulong)matrix[0,2]; break;
                case  6: mask = (ulong)matrix[2,2]; break;
                case  7: mask = (ulong)matrix[4,2]; break;
                case  8: mask = (ulong)matrix[1,1]; break;
                case  9: mask = (ulong)matrix[3,1]; break;
                case 10: mask = (ulong)matrix[0,0]; break;
                case 11: mask = (ulong)matrix[2,0]; break;
                case 12: mask = (ulong)matrix[4,0]; break;
                default: break;
            }

            newMask |= mask << i * 4;
        }

        if (true == isSmall)
        {
            newMask |= 1ul << (4 * i);
        }

        return newMask;
    }
    private int[,] GetHeightMatrixRotated(int rot)
    {
        var matrix = new int[5, 5];
        var flag = naviMask;
        for (var i = 0; i < 13; i++)
        {
            var h = (int)(flag & 0b1111);

            switch (i)
            {
                case  0: matrix[0,4] = h; break;
                case  1: matrix[2,4] = h; break;
                case  2: matrix[4,4] = h; break;
                case  3: matrix[1,3] = h; break;
                case  4: matrix[3,3] = h; break;
                case  5: matrix[0,2] = h; break;
                case  6: matrix[2,2] = h; break;
                case  7: matrix[4,2] = h; break;
                case  8: matrix[1,1] = h; break;
                case  9: matrix[3,1] = h; break;
                case 10: matrix[0,0] = h; break;
                case 11: matrix[2,0] = h; break;
                case 12: matrix[4,0] = h; break;
            }
            
            flag >>= 4;
        }

        return matrix;
    }
    private int[,] RotateMatrix(int[,] matrix, int rot)
    {
        if (0 == rot)
        {
            return matrix;
        }
        
        var n = matrix.GetLength(0); // 행렬 크기
        var rotated = new int[n, n];
        
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                switch (rot)
                {
                    case 270:
                        rotated[j, n - 1 - i] = matrix[i, j];
                        break;
                    case 180:
                        rotated[n - 1 - i, n - 1 - j] = matrix[i, j];
                        break;
                    case 90:
                        rotated[n - 1 - j, i] = matrix[i, j];
                        break;
                    default:
                        break;
                }
            }
        }

        return rotated;
    }
    
    
    // Info
}
