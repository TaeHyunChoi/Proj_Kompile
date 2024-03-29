using UnityEngine;
using static Public;
using CMathf;
using CDataStructure;

/// <summary> Parser related to Voxel </summary>
public static class PVoxel
{
    public static Vector3 GetPivot(Vector3 point, int exponent = 2)
    {
        float cx = CMath.FloorToInt(point.x * TILE_INVERSE, exponent) * TILE_SIZE;
        float cy = CMath.FloorToInt(point.y * TILE_INVERSE, exponent) * TILE_SIZE;
        float cz = CMath.FloorToInt(point.z * TILE_INVERSE, exponent) * TILE_SIZE;

        return new Vector3(cx, cy, cz);
    }
    public static Vector3 GetPivot(int key)
    {
        //using static Public;
        //Public.TILE_SIZE = 0.5f;

        float x = (key >> 16)           * TILE_SIZE;
        float y = ((key >> 8) & 0x00FF) * TILE_SIZE;
        float z = (key & 0xFF)          * TILE_SIZE;

        return new Vector3(x, y, z);
    }
    public static int GetKey(Vector3 point)
    {
        Vector3 pivot = GetPivot(point);
        return (int)(pivot.x * TILE_INVERSE) << 16 | (int)(pivot.y * TILE_INVERSE) << 8 | (int)(pivot.z * TILE_INVERSE);
    }
    //public static byte GetMoveFlag(Vector3 diff)
    //{
    //    byte quarant = GetMoveQuarant(diff);
    //    return (byte)(1 << quarant);
    //}
    public static int GetMoveFlag(Vector3 diff)
    {
        //8분면으로 나눠야 한다...
        int flag = 0;
        if (diff.x > TILE_HALF) { flag |= 0b_100; }
        if (diff.z > TILE_HALF) { flag |= 0b_010; }

        //positive
        if (flag == 0b000 || flag == 0b110)
        {
            if (diff.z > diff.x)
            {
                flag |= 0b001;
            }

            switch (flag)
            {
                case 0b000: return 1 << 0;
                case 0b001: return 1 << 7;
                case 0b110: return 1 << 3;
                case 0b111: return 1 << 4;
            }
        }
        else
        {
            if (diff.z > -diff.x + TILE_SIZE)
            {
                flag |= 0b001;
            }
            switch (flag)
            {
                case 0b100: return 1 << 1;
                case 0b101: return 1 << 2;
                case 0b010: return 1 << 6;
                case 0b011: return 1 << 5;
            }
        }

        return -1;
    }

    public static byte GetMoveQuarant(Vector3 diff)
    {
        byte q = 0;
        q |= (byte)((diff.z > diff.x) ? 0b_10 : 0);                 // y =  x 기준으로 비교
        q |= (byte)((diff.z > -diff.x + TILE_SIZE) ? 0b_01 : 0);    // y = -x 기준으로 비교

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
        Vector3 pivot = GetPivot(point);
        int quarant = GetMoveQuarant(point - pivot);

        //set y value
        Vector3 p0   = pivot + new Vector3(0, voxel.GetYValue((quarant + 4) % 4), 0) * TILE_SIZE;
        Vector3 p1   = pivot + new Vector3(0, voxel.GetYValue((quarant + 5) % 4), 0) * TILE_SIZE;
        Vector3 pMid = pivot + new Vector3(TILE_HALF, voxel.GetYValue(4) * TILE_SIZE, TILE_HALF);

        //set x,z value
        switch (quarant)
        {
            case 0:
                p0 += new Vector3(1, 0, 0) * TILE_SIZE;
                p1 += new Vector3(1, 0, 1) * TILE_SIZE;
                break;
            case 1:
                p0 += new Vector3(1, 0, 1) * TILE_SIZE;
                p1 += new Vector3(0, 0, 1) * TILE_SIZE;
                break;
            case 2:
                p0 += new Vector3(0, 0, 1) * TILE_SIZE;
                //p1 += Vector3.zero;
                break;
            case 3:
                //p0 += Vector3.zero;
                p1 += new Vector3(1, 0, 0) * TILE_SIZE;
                break;
        }

        //get normal
        p0 = CMath.FloorToVector(p0, 3);
        p1 = CMath.FloorToVector(p1, 3);
        pMid = CMath.FloorToVector(pMid, 3);

        Vector3 normal = Vector3.Cross(p1 - pMid, p0 - pMid);
        normal.Normalize();
        normal = CMath.FloorToVector(normal, 3);

        //(cached) vector equation of the plane
        float y_inverse = 1f;
        switch (normal.y)
        {
            case 0.577f: y_inverse = 1.733f; break;
            case 0.707f: y_inverse = 1.414f; break;
            case 1.000f: return pivot.y;
        }

        float y = -(normal.x * point.x + normal.z * point.z - Vector3.Dot(normal, pMid)) * y_inverse;

        //The y value may be out of range due to floating point, etc., so process it again
        if (y < pivot.y)
            y = pivot.y;
        else if (y > pivot.y + TILE_SIZE)
            y = pivot.y + TILE_SIZE;

        return CMath.Floor(y, 3);
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
            x = CMath.Floor(x - diff, exponent);
        }
        else if (dist * 0.9f <= diff && diff < dist)
        {
            x += (dist - diff);
            x = CMath.Floor(x + (dist - diff), exponent);
        }

        diff = y % dist;
        if (0 < diff & diff <= dist * 0.1f)
        {
            y -= diff;
            y = CMath.Floor(y - diff, exponent);
        }
        else if (dist * 0.9f <= diff && diff < dist)
        {
            y += (dist - diff);
            y = CMath.Floor(y + (dist - diff), exponent);
        }

        diff = z % dist;
        if (0 < diff & diff <= dist * 0.1f)
        {
            z -= diff;
            z = CMath.Floor(z - diff, exponent);
        }
        else if (dist * 0.9f <= diff && diff < dist)
        {
            z += (dist - diff);
            z = CMath.Floor(z + (dist - diff), exponent);
        }

        return new Vector3(x, y, z);
    }
    public static int GetHeightFlag(Vector3 diff)
    {
        int x = CMath.FloorToInt(diff.x * TILE_HALF_INVERSE,   0);
        int y = CMath.FloorToInt(diff.y * TILE_QUATER_INVERSE, 0);
        int z = CMath.FloorToInt(diff.z * TILE_HALF_INVERSE,   0);

        return y << (x + z * 3) * 3;
    }
    public static int GetHeightFlag(Vector3 diff0, Vector3 diff1, Vector3 diff2)
    {
        int flag = 0;
        flag |= GetHeightFlag(diff0);
        flag |= GetHeightFlag(diff1);
        flag |= GetHeightFlag(diff2);

        return flag;
    }

    public static void DebugTileData(int key, Tile_t2 tile)
    {
        string stringData = string.Format("l:{0}, s:{1}, h:{2}, m:{3}",
                                            tile.Layer,
                                            System.Convert.ToString(tile.Status, 2),
                                            System.Convert.ToString(tile.Height, 2),
                                            System.Convert.ToString(tile.Move, 2));

        Debug.Log(string.Format($"{GetPivot(key)} " + stringData));
    }
}
