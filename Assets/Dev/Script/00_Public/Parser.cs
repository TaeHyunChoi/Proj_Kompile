using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public static class Parser
{
    public static Vector3 GetVoxelPoint(Vector3 pos)
    {
        float cx = Mathf.Floor(pos.x * VOXEL_SIZE_INVERT) * VOXEL_SIZE;
        float cy = Mathf.Floor(pos.y * VOXEL_SIZE_INVERT) * VOXEL_SIZE;
        float cz = Mathf.Floor(pos.z * VOXEL_SIZE_INVERT) * VOXEL_SIZE;

        return new Vector3(cx, cy, cz);
    }
    public static bool IsMovable(Dictionary<int,Voxel_t> data, Vector3 voxelPoint, Vector3 dir)
    {
        //get radix
        int bx = (byte)(voxelPoint.x * VOXEL_SIZE_INVERT);
        int by = (byte)(voxelPoint.y * VOXEL_SIZE_INVERT);
        int bz = (byte)(voxelPoint.z * VOXEL_SIZE_INVERT);
        int radix = (bx << 16) | (by << 8) | bz;

        //get sub:진입방향(dir)의 반대로 뒤집기
        int d = 0;
        if (dir.x > 0) { d |= 1 << 2; }
        if (dir.y > 0) { d |= 1 << 1; }
        if (dir.z > 0) { d |= 1 << 0; }

        int shift = -1;
        switch (d)
        {                                   //진입 방향  //타입 마스크
            case 0b_000: shift =  6; break; //[-, -, -]   0
            case 0b_100: shift =  4; break; //[+, -, -]   2
            case 0b_001: shift =  2; break; //[-, -, +]   4
            case 0b_101: shift =  0; break; //[+, -, +]   6

            case 0b_010: shift = 14; break; //[-, +, -]   8
            case 0b_110: shift = 12; break; //[+, +, -]  10
            case 0b_011: shift = 10; break; //[-, +, +]  12
            case 0b_111: shift =  8; break; //[+, +, +]  14
        }

        int sub = data[radix].SubVoxel;
        sub &= 0b11 << shift;
        return (sub >> shift) == (int)VoxelType.Movable;
    }

    //     public static bool IsMovePossible(Dictionary<int,Voxel_t> data, Vector3 pos)
    // {
    //     // Parse from position to Voxel
    //     float cx = Mathf.Floor(pos.x * VOXEL_SIZE_INVERT) * VOXEL_SIZE;
    //     float cy = Mathf.Floor(pos.y * VOXEL_SIZE_INVERT) * VOXEL_SIZE;
    //     float cz = Mathf.Floor(pos.z * VOXEL_SIZE_INVERT) * VOXEL_SIZE;

    //     byte bx = (byte)(cx / VOXEL_SIZE);
    //     byte by = (byte)(cy / VOXEL_SIZE);
    //     byte bz = (byte)(cz / VOXEL_SIZE);

    //     Vector3 voxelPoint = new Vector3(cx, cy, cz);
    //     int radix = (bx << 16) | (by << 8) | bz;

    //     if(!data.ContainsKey(radix))
    //     {
    //         return false;
    //     }

    //     int sub = data[radix].Sub;

    //     // Check Move Possible (by sub-voxels)
    //     Vector3 diff = pos - voxelPoint;
    //     float halfVoxelSize = VOXEL_SIZE * 0.5f;

    //     byte d = 0, shift = 0;
    //     if (diff.x > halfVoxelSize) { d |= 1 << 2; }
    //     if (diff.y > halfVoxelSize) { d |= 1 << 1; }
    //     if (diff.z > halfVoxelSize) { d |= 1 << 0; }

    //     switch (d)
    //     {
    //         case 0b_000: shift =  0; break; //[-, -, -]
    //         case 0b_100: shift =  2; break; //[+, -, -]
    //         case 0b_001: shift =  4; break; //[-, -, +]
    //         case 0b_101: shift =  6; break; //[+, -, +]
    //         case 0b_010: shift =  8; break; //[-, +, -]
    //         case 0b_110: shift = 10; break; //[+, +, -]
    //         case 0b_011: shift = 12; break; //[-, +, +]
    //         case 0b_111: shift = 14; break; //[+, +, +]
    //     }

    //     sub &= 0b11 << shift;
    //     return (sub>>shift) == (int)VoxelType.Movable;
    // }

}
