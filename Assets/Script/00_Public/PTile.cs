using UnityEngine;
//using static Public;
using CMathf;
using DevDataType;
using System;
using System.Collections.Generic;

[Flags]
public enum TileFeature : byte
{
    Inner = 1 << 0,
    Small = 1 << 1,


    Trans = 1 << 5
}
public enum TileSize
{
    Default,
    Half,
    Quater,
    Inverse,
    Default_Inverse,
    Half_inverse,
    Quater_inverse
}

/// <summary> Parser related to Tile </summary> /// 
public static class PTile
{
    public const float SIZE = 1f;
    public const float SIZE_INVERSE = 1f / SIZE;
    public const float SIZE_HALF = 0.5f * SIZE;
    public const float SIZE_HALF_INVERSE = 1f / SIZE_HALF;

    //// get
    public static Vector3 GetPivot(int key, float size)
    {
        float x = (key & 0xFF_00_00) >> 16;
        float y = (key & 0x00_FF_00) >> 8;
        float z = (key & 0x00_00_FF) >> 0;

        return new Vector3(x, y, z) * size;
    }
    public static Vector3 GetPivot(Vector3 point, float size)
    {
        float size_inverse = 1 / size;
        float cx = CMath.FloorToInt(point.x * size_inverse, 2) * size;
        float cy = CMath.FloorToInt(point.y * size_inverse, 2) * size;
        float cz = CMath.FloorToInt(point.z * size_inverse, 2) * size;

        return new Vector3(cx, cy, cz);
    }
    public static int GetKey(Vector3 point, float size)
    {
        Vector3 pivot = GetPivot(point, size);
        size = 1 / size;
        return (int)(pivot.x * size) << 16 | (int)(pivot.y * size) << 8 | (int)(pivot.z * size);
    }
    public static float GetScale(TileSize type, float scale)
    {
        // for using cache data

        float size;
        
        switch (type)
        {
            case TileSize.Default:           size = SIZE;                break;
            case TileSize.Half:              size = SIZE_HALF;           break;
            case TileSize.Default_Inverse:   size = SIZE_INVERSE;        break;
            case TileSize.Half_inverse:      size = SIZE_HALF_INVERSE;   break;
            default: return -1f;
        }
        if (type > TileSize.Inverse)
        {
            scale = 1 / scale;
        }

        return size * scale;
    }
    public static Vector3 SnappingPoint(Vector3 p, float dist, int exponent)
    {
        float x = p.x;
        float y = p.y;
        float z = p.z;
        float diff;
        diff = x % dist;

        //Similar to rounding, but the standard is different for each dist, not 0.5f.
        if (0 < diff & diff <= dist * 0.1f)
        {
            x -= diff;
        }
        else if (dist * 0.9f <= diff && diff < dist)
        {
            x += (dist - diff);
        }

        diff = y % dist;
        if (0 < diff & diff <= dist * 0.1f)
        {
            y -= diff;
        }
        else if (dist * 0.9f <= diff && diff < dist)
        {
            y += (dist - diff);
        }

        diff = z % dist;
        if (0 < diff & diff <= dist * 0.1f)
        {
            z -= diff;
        }
        else if (dist * 0.9f <= diff && diff < dist)
        {
            z += (dist - diff);
        }

        return CMath.FloorToVector(new Vector3(x, y, z), exponent);
    }

    ////maybe use later 
    /*
    public static Vector3 GetPivot(int key, float size)
    {
        float x = (key & 0xFF_00_00) >> 16;
        float y = (key & 0x00_FF_00) >> 8;
        float z = (key & 0x00_00_FF) >> 0;

        return new Vector3(x, y, z) * size;
    }
    public static Vector3 GetPivot(Vector3 point, int exponent = 2)
    {
        float cx = CMath.FloorToInt(point.x * SIZE_INVERSE, exponent) * SIZE;
        float cy = CMath.FloorToInt(point.y * SIZE_INVERSE, exponent) * SIZE;
        float cz = CMath.FloorToInt(point.z * SIZE_INVERSE, exponent) * SIZE;

        return new Vector3(cx, cy, cz);
    }
    public static int GetKey(Vector3 point)
    {
        Vector3 pivot = GetPivot(point);
        return (int)(pivot.x * SIZE_INVERSE) << 16 | (int)(pivot.y * SIZE_INVERSE) << 8 | (int)(pivot.z * SIZE_INVERSE);
    }
    public static byte GetMoveQuarant(Vector3 diff)
    {
        byte q = 0;
        q |= (byte)((diff.z > diff.x) ? 0b_10 : 0);                 // y =  x �������� ��
        q |= (byte)((diff.z > -diff.x + SIZE) ? 0b_01 : 0);    // y = -x �������� ��

        switch (q)
        {
            case 0b_01: q = 1; break;
            case 0b_11: q = 2; break;
            case 0b_10: q = 3; break;
            case 0b_00: q = 0; break;
        }

        return q;
    }
    public static float GetYValue(Tile_t voxel, Vector3 point)
    {
        Debug.Log("Need to dev");
        return 0f;
        //Vector3 pivot = GetPivot(point);
        //int quarant = GetMoveQuarant(point - pivot);

        ////set y value
        //Vector3 p0   = pivot + new Vector3(0, voxel.GetYValue((quarant + 4) % 4), 0) * TILE_SIZE;
        //Vector3 p1   = pivot + new Vector3(0, voxel.GetYValue((quarant + 5) % 4), 0) * TILE_SIZE;
        //Vector3 pMid = pivot + new Vector3(TILE_HALF, voxel.GetYValue(4) * TILE_SIZE, TILE_HALF);

        ////set x,z value
        //switch (quarant)
        //{
        //    case 0:
        //        p0 += new Vector3(1, 0, 0) * TILE_SIZE;
        //        p1 += new Vector3(1, 0, 1) * TILE_SIZE;
        //        break;
        //    case 1:
        //        p0 += new Vector3(1, 0, 1) * TILE_SIZE;
        //        p1 += new Vector3(0, 0, 1) * TILE_SIZE;
        //        break;
        //    case 2:
        //        p0 += new Vector3(0, 0, 1) * TILE_SIZE;
        //        //p1 += Vector3.zero;
        //        break;
        //    case 3:
        //        //p0 += Vector3.zero;
        //        p1 += new Vector3(1, 0, 0) * TILE_SIZE;
        //        break;
        //}

        ////get normal
        //p0 = CMath.FloorToVector(p0, 3);
        //p1 = CMath.FloorToVector(p1, 3);
        //pMid = CMath.FloorToVector(pMid, 3);

        //Vector3 normal = Vector3.Cross(p1 - pMid, p0 - pMid);
        //normal.Normalize();
        //normal = CMath.FloorToVector(normal, 3);

        ////(cached) vector equation of the plane
        //float y_inverse = 1f;
        //switch (normal.y)
        //{
        //    case 0.577f: y_inverse = 1.733f; break;
        //    case 0.707f: y_inverse = 1.414f; break;
        //    case 1.000f: return pivot.y;
        //}

        //float y = -(normal.x * point.x + normal.z * point.z - Vector3.Dot(normal, pMid)) * y_inverse;

        ////The y value may be out of range due to floating point, etc., so process it again
        //if (y < pivot.y)
        //    y = pivot.y;
        //else if (y > pivot.y + TILE_SIZE)
        //    y = pivot.y + TILE_SIZE;

        //return CMath.Floor(y, 3);
    }
        public static bool IsLinkedOrNot(Dictionary<int, Tile_t> sample, int keyMy, int indexLink, int y)
    {
        //init quarant
        int quarantMy, quarantTarget;
        int keyTarget;

        switch (indexLink)
        {
            case 1:
                quarantMy = 0;
                quarantTarget = 10;
                keyTarget = keyMy + (0 << 16) + y * (1 << 8) - (1 << 0);
                break;
            case 2:
                quarantMy = 4;
                quarantTarget = 14;
                keyTarget = keyMy + (0 << 16) + y * (1 << 8) - (1 << 0);
                break;
            case 4:
                quarantMy = 5;
                quarantTarget = 3;
                keyTarget = keyMy + (1 << 16) + y * (1 << 8) + (0 << 0);
                break;
            case 5:
                quarantMy = 13;
                quarantTarget = 11;
                keyTarget = keyMy + (1 << 16) + y * (1 << 8) + (0 << 0);
                break;
            case 7:
                quarantMy = 14;
                quarantTarget = 4;
                keyTarget = keyMy + (0 << 16) + y * (1 << 8) + (1 << 0);
                break;
            case 8:
                quarantMy = 10;
                quarantTarget = 0;
                keyTarget = keyMy + (0 << 16) + y * (1 << 8) + (1 << 0);
                break;
            case 10:
                quarantMy = 11;
                quarantTarget = 13;
                keyTarget = keyMy - (1 << 16) + y * (1 << 8) + (0 << 0);
                break;
            case 11:
                quarantMy = 3;
                quarantTarget = 5;
                keyTarget = keyMy - (1 << 16) + y * (1 << 8) + (0 << 0);
                break;
            default:
                return false;
        }

        //check my tile
        if (false == sample.TryGetValue(keyMy, out Tile_t tileMy)
            || false == tileMy.IsMovable(quarantMy))
        {
            return false;
        }

        //check target tile
        if (false == sample.TryGetValue(keyTarget, out Tile_t tileTarget)
            || false == tileTarget.IsMovable(quarantTarget))
        {
            return false;
        }

        //compare height
        int diff = y * 4;
        tileMy.GetHeightMask(quarantMy, out int p00, out int p01);
        tileTarget.GetHeightMask(quarantTarget, out int p10, out int p11);
        if (p00 != p10 + diff || p01 != p11 + diff)
        {
            return false;
        }

        return true;
    }
    //*/
}