using System;
using System.Collections.Generic;
using UnityEngine;
using CMathf;
using static Index.IDxTile;
using static Index.IDxInput;

[Flags]
public enum TileStatus : byte
{ 
    None   = 0,
    Locked = 1 << 0
}
[Flags]
public enum TileTrigger : ushort
{
    None      = 0,
    Scale = 1 << SHIFT_TRIGGER_SCALE,
    Layer     = 1 << SHIFT_TRIGGER_LAYER,
    Interact  = 1 << SHIFT_TRIGGER_INTERACT
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
public static class TileUtility
{
    public static Vector3 GetPivot(int key, float scale)
    {
        float x = 0f, y = 0f, z = 0f;
        if (0 != ((key >> SHIFT_KEY_SCALE) & 0x1))
        {
            x += 0.125f;
        }

        key &= ~(1 << SHIFT_KEY_SCALE);
        x += ((key >> SHIFT_KEY_X) & 0xFF) * scale;
        y += ((key >> SHIFT_KEY_Y) & 0x0F) * scale;
        z += ((key >> SHIFT_KEY_Z) & 0xFF) * scale;

        return new Vector3(x, y, z);
    }
    public static Vector3 GetPivot(Vector3 point, float scale)
    {
        float scale_inverse = GetScale(TileSize.Default_Inverse, scale);

        int cx = CMath.FloorToInt(point.x * scale_inverse, 3);
        int cy = CMath.FloorToInt(point.y * scale_inverse, 3);
        int cz = CMath.FloorToInt(point.z * scale_inverse, 3);
        Vector3 pivot = new Vector3(cx, cy, cz) * scale;

        if (1f != scale)
        {
            float scale_quater = scale * 0.25f;
            float x = CMath.FloorToInt((point.x - scale_quater) * scale_inverse, 3);
            x *= scale;
            x += scale_quater;

            pivot = new Vector3(x, pivot.y, pivot.z);
            //Debug.Log($"{pivot:F3}");
        }

        return pivot;
    }
    public static int GetKey(int layer, Vector3 point, float scale)
    {
        if (0 > point.x || 128 < point.x || 0 > point.z || 128 < point.z)
        {
            return -1;
        }

        Vector3 pivot = GetPivot(point, scale);
        float scale_inverse = GetScale(TileSize.Default_Inverse, scale);

        int key = layer << SHIFT_KEY_LAYER;
        if (0 != pivot.x % 1f) 
        { 
            key |= 1 << SHIFT_KEY_SCALE; 
        }

        key |= (int)(pivot.x * scale_inverse) << SHIFT_KEY_X;
        key |= (int)(pivot.y * scale_inverse) << SHIFT_KEY_Y;
        key |= (int)(pivot.z * scale_inverse) << SHIFT_KEY_Z;

        return key;
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
            case TileSize.Quater:            size = SIZE_QUATER;         break;
            case TileSize.Quater_inverse:    size = SIZE_QUATER_INVERSE; break;
            default: return 0f;
        }
        if (type > TileSize.Inverse)
        {
            scale = 1 / scale;
        }

        return size * scale;
    }
    public static int GetTriangleIndex(Vector3 diff, float scale_half)
    {
        int quarant = 0;
        if (diff.x >= scale_half)
        {
            quarant |= 0b_01;
            diff -= new Vector3(scale_half, 0, 0);
        }
        if (diff.z >= scale_half)
        {
            quarant |= 0b_10;
            diff -= new Vector3(0, 0, scale_half);
        }
        quarant *= 4;

        int equation = 0;
        if (diff.z >= diff.x)
        {
            equation |= 0b01;
        }
        if (diff.z >= -diff.x + scale_half)
        {
            equation |= 0b10;
        }

        switch (equation)
        {
            case 0b00: return quarant;
            case 0b10: return quarant + 1;
            case 0b11: return quarant + 2;
            case 0b01: return quarant + 3;
        }

        return -1;
    }
    public static Vector3 GetDirection(int input)
    {
        Vector3 dir = Vector3.zero;
        if (true == Compare(input, UP)    || true == Compare(input, UP_HOLD))    { dir += Vector3.forward; }
        if (true == Compare(input, DOWN)  || true == Compare(input, DOWN_HOLD))  { dir += Vector3.back; }
        if (true == Compare(input, LEFT)  || true == Compare(input, LEFT_HOLD))  { dir += Vector3.left; }
        if (true == Compare(input, RIGHT) || true == Compare(input, RIGHT_HOLD)) { dir += Vector3.right; }

        dir.Normalize();
        return CMath.FloorToVector(dir, 3);
    }
    public static int GetKey_FromRelativeCoord(Dictionary<int, DataType.Tile_t> map, int key, int x, int z)
    {
        int keyLink = key + x * (1 << SHIFT_KEY_X) + z * (1 << SHIFT_KEY_Z);

        // y = 0
        if (true == map.ContainsKey(keyLink))
        {
            return keyLink;
        }
        //y + 1
        if (true == map.ContainsKey(keyLink + (1 << 8)))
        {
            
            return keyLink += (1 << 8);
        }
        // y - 1
        if (true == map.ContainsKey(keyLink - (1 << 8)))
        {
            
            return keyLink -= (1 << 8);
        }

        return -1;
    }
    public static Vector3[] GetQuarantPoints(Vector3 pivot, float scale, long flagHeight, int quarant)
    {
        //인덱스를 미숙하게+많이 짜서 고생하는구만..
        //왼손 좌표계...
        int i0, i1, i2;
        switch (quarant)
        {
            case  0: i0 = 0; i1 = 9; i2 = 1; break;
            case  1: i0 = 1; i1 = 9; i2 = 4; break;
            case  2: i0 = 3; i1 = 4; i2 = 9; break;
            case  3: i0 = 0; i1 = 3; i2 = 9; break;

            case  4: i0 = 1; i1 = 10; i2 = 2; break;
            case  5: i0 = 2; i1 = 10; i2 = 5; break;
            case  6: i0 = 4; i1 = 5; i2 = 10; break;
            case  7: i0 = 1; i1 = 4; i2 = 10; break;

            case  8: i0 = 3; i1 = 11; i2 = 4; break;
            case  9: i0 = 4; i1 = 11; i2 = 7; break;
            case 10: i0 = 6; i1 = 7; i2 = 11; break;
            case 11: i0 = 3; i1 = 6; i2 = 11; break;

            case 12: i0 = 4; i1 = 12; i2 = 5; break;
            case 13: i0 = 5; i1 = 12; i2 = 8; break;
            case 14: i0 = 7; i1 = 8; i2 = 12; break;
            case 15: i0 = 4; i1 = 7; i2 = 12; break;

            default: return null;
        }

        Vector3 p0 = GetPoint(pivot, flagHeight, i0, scale);
        Vector3 p1 = GetPoint(pivot, flagHeight, i1, scale);
        Vector3 p2 = GetPoint(pivot, flagHeight, i2, scale);

        return new Vector3[3] { p0, p1, p2 };
    }
    private static Vector3 GetPoint(Vector3 pivot, long flagHeight, int index, float scale)
    {
        float scale_half    = scale * 0.5f;
        float scale_quater  = scale * 0.25f;

        float y = (flagHeight >> (index * 3)) & 0b111;
        y *= scale_quater;

        switch (index)
        {
            case  0: return pivot;
            case  1: return pivot + new Vector3(scale_half, y, 0);
            case  2: return pivot + new Vector3(scale, y, 0);

            case  3: return pivot + new Vector3(0, y, scale_half);
            case  4: return pivot + new Vector3(scale_half, y, scale_half);
            case  5: return pivot + new Vector3(scale, y, scale_half);

            case  6: return pivot + new Vector3(0, y, scale);
            case  7: return pivot + new Vector3(scale_half, y, scale);
            case  8: return pivot + new Vector3(scale, y, scale);

            case  9: return pivot + new Vector3(scale_quater, y, scale_quater);
            case 10: return pivot + new Vector3(scale_half + scale_quater, y, scale_quater);
            case 11: return pivot + new Vector3(scale_quater, y, scale_half + scale_quater);
            case 12: return pivot + new Vector3(scale_half + scale_quater, y, scale_half + scale_quater);
        }

        return Vector3.zero;
    }
    public static Vector3 SnappingPoint(Vector3 p, float dist, int exponent)
    {
        float x = p.x;
        float y = p.y;
        float z = p.z;
        float diff;

        //Similar to rounding, but the standard is different for each dist, not 0.5f.
        diff = x % dist;
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
}

public struct TriangleCollision
{
    public Vector3 A, B, C; // x,z로만 판별
    public int key;
    public int index;

    public TriangleCollision(int key, Vector3 pivot, int indexTriangle, float scale)
    {
        A = B = C = pivot;
        this.key = key;
        index = indexTriangle;

        float scale_quater = scale * 0.25f;
        switch (index % 4)
        {
            case 0:
                A += new Vector3(1f, 0f, 1f) * scale_quater;
                B += new Vector3(2f, 0f, 0f) * scale_quater;

                break;
            case 1:
                A += new Vector3(1f, 0f, 1f) * scale_quater;
                B += new Vector3(2f, 0f, 0) * scale_quater;
                C += new Vector3(2f, 0f, 2f) * scale_quater;
                break;
            case 2:
                A += new Vector3(1f, 0f, 1f) * scale_quater;
                B += new Vector3(0, 0f, 2f) * scale_quater;
                C += new Vector3(2f, 0f, 2f) * scale_quater;
                break;
            case 3:
                A += new Vector3(1f, 0f, 1f) * scale_quater;
                B += new Vector3(0, 0f, 2f) * scale_quater;

                break;
        }

        switch ((int)(index * 0.25f))
        {
            case 1:
                A += new Vector3(2f, 0f, 0) * scale_quater;
                B += new Vector3(2f, 0f, 0) * scale_quater;
                C += new Vector3(2f, 0f, 0) * scale_quater;
                break;
            case 2:
                A += new Vector3(0, 0f, 2f) * scale_quater;
                B += new Vector3(0, 0f, 2f) * scale_quater;
                C += new Vector3(0, 0f, 2f) * scale_quater;
                break;
            case 3:
                A += new Vector3(2f, 0f, 2f) * scale_quater;
                B += new Vector3(2f, 0f, 2f) * scale_quater;
                C += new Vector3(2f, 0f, 2f) * scale_quater;
                break;
        }
    }

    public bool IsIntersected(Vector3 center, float radius)
    {
        Vector2 center2D = new Vector2(center.x, center.z);
        Vector2 A2d = new Vector2(A.x, A.z);
        Vector2 B2d = new Vector2(B.x, B.z);
        Vector2 C2d = new Vector2(C.x, C.z);

        if (PointInTriangle(center2D, A2d, B2d, C2d))
        {
            return true;
        }

        // 삼각형의 각 꼭짓점이 원 내부에 있는지 확인
        if (IsPointInsideCircle(A2d, center2D, radius) ||
            IsPointInsideCircle(B2d, center2D, radius) ||
            IsPointInsideCircle(C2d, center2D, radius))
        {
            return true;
        }


        // 삼각형의 각 변과 원의 교차 확인
        if (IsCircleLineIntersect(center2D, radius, A2d, B2d) ||
            IsCircleLineIntersect(center2D, radius, B2d, C2d) ||
            IsCircleLineIntersect(center2D, radius, C2d, A2d))
        {
            return true;
        }

        return false;
    }
    private bool IsPointInsideCircle(Vector2 point, Vector2 circleCenter, float radius)
    {
        return (point - circleCenter).sqrMagnitude < radius * radius;
    }
    private bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
        float t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

        if ((s < 0) != (t < 0))
            return false;

        float A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
        if (A < 0.0)
        {
            s = -s;
            t = -t;
            A = -A;
        }
        return s > 0 && t > 0 && (s + t) < A;
    }
    private bool IsCircleLineIntersect(Vector2 circleCenter, float radius, Vector2 A, Vector2 B)
    {
        // 선분 AB의 방향 벡터를 계산합니다.
        Vector2 d = B - A;

        // 원의 중심에서 점 A까지의 벡터를 계산합니다.
        Vector2 f = A - circleCenter;

        // 2차 방정식의 계수 a, b, c를 계산합니다. 이 방정식은 선분과 원의 교차 조건을 나타냅니다.
        float a = Vector2.Dot(d, d); // d 벡터의 길이의 제곱
        float b = 2 * Vector2.Dot(f, d); // f와 d 벡터의 내적을 2배 한 값
        float c = Vector2.Dot(f, f) - radius * radius; // f 벡터의 길이의 제곱에서 원의 반지름 제곱을 뺀 값

        // 판별식을 계산합니다. 이 값이 양수라면 근이 실수로 존재함을 의미합니다.
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            // 판별식이 음수이면, 선분과 원은 서로 교차하지 않습니다.
            return false;
        }
        else
        {
            // 판별식의 제곱근을 구하여 실제 근을 찾습니다.
            discriminant = Mathf.Sqrt(discriminant);

            // 근의 공식을 사용하여 두 근을 계산합니다.
            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);

            // 두 근 중 하나라도 선분의 파라미터 0과 1 사이에 있으면, 선분이 원과 교차합니다.
            if (t1 >= 0 && t1 <= 1 || t2 >= 0 && t2 <= 1)
                return true;

            // 그렇지 않다면 교차하지 않습니다.
            return false;
        }
    }
}
public static class TriangleUtility
{
    private static TriangleCollision[] triangles = new TriangleCollision[16]; //0:본인, 1~:비교대상
    private static int index;

    public static void SetTriangleArray(Dictionary<int, DataType.Tile_t> map, int triangle, int key, Vector3 pivot, float scale)
    {
        index = 0;
        switch (triangle)
        {
            case 0:
                //params[] 쓰면 편할 텐데 힙 메모리는 가능하면 지양하기로.
                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);

                //neighbor: z-1
                int keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: -1);
                Vector3 pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

                //neighbor: x-1, z-1
                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: -1);
                pivotNeighbor = pivot + new Vector3(-1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                //neighbor: x-1
                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                break;

            case 1:
                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);
                break;

            case 2:
                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                break;

            case 3:
                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: -1);
                pivotNeighbor = pivot + new Vector3(-1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);

                break;
            case 4:
                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: -1);
                pivotNeighbor = pivot + new Vector3(1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                break;
            case 5:
                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: -1);
                pivotNeighbor = pivot + new Vector3(+1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                break;

            case 6:
                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                break;
            case 7:
                triangles[index++] = new TriangleCollision(key, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);
                break;
            case 8:
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                break;
            case 9:
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
                break;
            case 10:
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: +1);
                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                break;
            case 11:
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: +1);
                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                break;
            case 12:
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                break;
            case 13:
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: +1);
                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                break;
            case 14:
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: +1, z: +1);
                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
                break;
            case 15:
                triangles[index++] = new TriangleCollision(key, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(key, pivot, 10, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, key, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
                break;
        }
    }
    public static bool IsMovable(Dictionary<int, DataType.Tile_t> map, Vector3 goal, float scale)
    {
        float dist = CMath.Floor(scale * SIZE_QUATER - Time.fixedDeltaTime, 3);
        for (int i = 0; i < index; ++i)
        {
            TriangleCollision triangle = triangles[i];

            if (false == triangle.IsIntersected(goal, dist))
            {
                continue;
            }
            if (false == map.TryGetValue(triangle.key, out DataType.Tile_t tileChecked))
            {
                return false;
            }
            if (false == tileChecked.IsMovable(triangle.index))
            {
                return false;
            }
        }

        return true;
    }
}
