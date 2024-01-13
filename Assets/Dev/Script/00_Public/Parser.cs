using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Public;

public static class Parser
{
    public static bool IsMovePossible(Dictionary<int,int> data, Vector3 pos)
    {
        // Parse from position to Voxel
        float cx = Mathf.Floor(pos.x * VOXEL_SIZE_INVERT) * VOXEL_SIZE;
        float cy = Mathf.Floor(pos.y * VOXEL_SIZE_INVERT) * VOXEL_SIZE;
        float cz = Mathf.Floor(pos.z * VOXEL_SIZE_INVERT) * VOXEL_SIZE;

        byte bx = (byte)(cx / VOXEL_SIZE);
        byte by = (byte)(cy / VOXEL_SIZE);
        byte bz = (byte)(cz / VOXEL_SIZE);

        Vector3 voxelPoint = new Vector3(cx, cy, cz);
        int radix = (bx << 16) | (by << 8) | bz;

        if(!data.ContainsKey(radix))
        {
            return false;
        }

        int sub = data[radix] & 0xFF;

        // Check Move Possible (by sub-voxels)
        Vector3 diff = pos - voxelPoint;
        float halfVoxelSize = VOXEL_SIZE * 0.5f;

        byte d = 0, shift = 0;
        if (diff.x > halfVoxelSize) { d |= 1 << 2; }
        if (diff.y > halfVoxelSize) { d |= 1 << 1; }
        if (diff.z > halfVoxelSize) { d |= 1 << 0; }

        switch (d)
        {
            case 0b_000: shift = 0; break; //[-, -, -]
            case 0b_100: shift = 1; break; //[+, -, -]
            case 0b_001: shift = 2; break; //[-, -, +]
            case 0b_101: shift = 3; break; //[+, -, +]
            case 0b_010: shift = 4; break; //[-, +, -]
            case 0b_110: shift = 5; break; //[+, +, -]
            case 0b_011: shift = 6; break; //[-, +, +]
            case 0b_111: shift = 7; break; //[+, +, +]
        }

        sub &= 1 << shift;
        return sub == 0;
    }
}
