using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using DataType;

public struct IsPossibleToMove
{
    //public Vector3 pivot;
    public int key;
    public byte triangle;
    public short movement;
    public float scale;

    public Vector3 position;
    public float radius;
}

[BurstCompile]
public struct CheckTriangleCollsion : IJobParallelFor
{
    [ReadOnly] public NativeArray<IsPossibleToMove> Targets;
    [WriteOnly] public NativeArray<bool> Collision;

    public void Execute(int index)
    {
        IsPossibleToMove data = Targets[index];

        bool result = true; // 상태값 (not_collided, movable, not_movable이 더 정확한 표현이긴 함;)
        if (true == IsCircleTriangleIntersect(data))
        {
            result = (0 != (data.movement & (1 << data.triangle)));
        }

        Collision[index] = result;
    }
    private bool IsCircleTriangleIntersect(IsPossibleToMove data)
    {
        Vector3 circleCenter = data.position;
        float radius = data.radius;
        float size = Index.IDxTile.SIZE_QUATER * data.scale;

        Vector3 pivot = PTile.GetPivot(data.key, data.scale);
        Vector3 A = pivot, B = pivot, C = pivot;

        switch (data.triangle % 4)
        {
            case 0:
                A += new Vector3(0.25f, 0f, 0.25f) * size;
                B += new Vector3(0.5f, 0f, 0f) * size;

                break;
            case 1:
                A += new Vector3(0.25f, 0f, 0.25f) * size;
                B += new Vector3(0.5f, 0, 0) * size;
                C += new Vector3(0.5f, 0, 0.5f) * size;
                break;
            case 2:
                A += new Vector3(0.25f, 0f, 0.25f) * size;
                B += new Vector3(0, 0, 0.5f) * size;
                C += new Vector3(0.5f, 0, 0.5f) * size;
                break;
            case 3:
                A += new Vector3(0.25f, 0f, 0.25f) * size;
                B += new Vector3(0, 0, 0.5f);

                break;
            default:
                return false;
        }

        if (1 <= data.triangle * 0.25f)
        {
            A += new Vector3(0.5f, 0, 0) * size;
            B += new Vector3(0.5f, 0, 0) * size;
            C += new Vector3(0.5f, 0, 0) * size;
        }
        else if (2 <= data.triangle * 0.25f)
        {
            A += new Vector3(0, 0, 0.5f) * size;
            B += new Vector3(0, 0, 0.5f) * size;
            C += new Vector3(0, 0, 0.5f) * size;
        }
        else if (3 <= data.triangle * 0.25f)
        {
            A += new Vector3(0.5f, 0, 0.5f) * size;
            B += new Vector3(0.5f, 0, 0.5f) * size;
            C += new Vector3(0.5f, 0, 0.5f) * size;
        }


        // 원의 중심이 삼각형 내부에 있는지 확인
        if (PointInTriangle(circleCenter, A, B, C))
            return true;

        // 삼각형의 각 꼭짓점이 원 내부에 있는지 확인
        if (IsPointInsideCircle(A, circleCenter, radius) ||
            IsPointInsideCircle(B, circleCenter, radius) ||
            IsPointInsideCircle(C, circleCenter, radius))
            return true;

        // 삼각형의 각 변과 원의 교차 확인
        if (IsCircleLineIntersect(circleCenter, radius, A, B) ||
            IsCircleLineIntersect(circleCenter, radius, B, C) ||
            IsCircleLineIntersect(circleCenter, radius, C, A))
            return true;

        return false;
    }
    private bool IsPointInsideCircle(Vector2 point, Vector2 circleCenter, float radius)
    {
        return (point - circleCenter).sqrMagnitude < radius * radius;
    }
    private bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        var s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
        var t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

        if ((s < 0) != (t < 0))
            return false;

        var A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
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

public struct Triangle
{
    public int key;
    public Vector3 pivot;
    public int triangle;
    public short movement;
    public float scale;

    public Vector3 center;
    public float radius;

    public Triangle(int key, Tile_t tile, Vector3 pivot, int triangle, Vector3 pos, float radius)
    {
        this.key = key;
        this.triangle = triangle;
        this.pivot = pivot;
        movement = (short)tile.Move;
        scale = tile.GetScale();
        center = pos;
        this.radius = radius;
    }

    public bool IsIntersected()
    {
        Vector3 A = pivot, B = pivot, C = pivot;
        float scale_quater = scale * 0.25f;

        switch (triangle % 4)
        {
            case 0:
                A += new Vector3(0.25f, 0f, 0.25f) * scale_quater;
                B += new Vector3(0.5f, 0f, 0f) * scale_quater;

                break;
            case 1:
                A += new Vector3(0.25f, 0f, 0.25f) * scale_quater;
                B += new Vector3(0.5f, 0, 0) * scale_quater;
                C += new Vector3(0.5f, 0, 0.5f) * scale_quater;
                break;
            case 2:
                A += new Vector3(0.25f, 0f, 0.25f) * scale_quater;
                B += new Vector3(0, 0, 0.5f) * scale_quater;
                C += new Vector3(0.5f, 0, 0.5f) * scale_quater;
                break;
            case 3:
                A += new Vector3(0.25f, 0f, 0.25f) * scale_quater;
                B += new Vector3(0, 0, 0.5f);

                break;
            default:
                return false;
        }

        if (1 <= triangle * 0.25f)
        {
            A += new Vector3(0.5f, 0, 0) * scale_quater;
            B += new Vector3(0.5f, 0, 0) * scale_quater;
            C += new Vector3(0.5f, 0, 0) * scale_quater;
        }
        else if (2 <= triangle * 0.25f)
        {
            A += new Vector3(0, 0, 0.5f) * scale_quater;
            B += new Vector3(0, 0, 0.5f) * scale_quater;
            C += new Vector3(0, 0, 0.5f) * scale_quater;
        }
        else if (3 <= triangle * 0.25f)
        {
            A += new Vector3(0.5f, 0, 0.5f) * scale_quater;
            B += new Vector3(0.5f, 0, 0.5f) * scale_quater;
            C += new Vector3(0.5f, 0, 0.5f) * scale_quater;
        }


        // 원의 중심이 삼각형 내부에 있는지 확인
        if (PointInTriangle(center, A, B, C))
            return true;

        // 삼각형의 각 꼭짓점이 원 내부에 있는지 확인
        if (IsPointInsideCircle(A, center, radius) ||
            IsPointInsideCircle(B, center, radius) ||
            IsPointInsideCircle(C, center, radius))
            return true;

        // 삼각형의 각 변과 원의 교차 확인
        if (IsCircleLineIntersect(center, radius, A, B) ||
            IsCircleLineIntersect(center, radius, B, C) ||
            IsCircleLineIntersect(center, radius, C, A))
            return true;

        return false;
    }
    private bool IsPointInsideCircle(Vector2 point, Vector2 circleCenter, float radius)
    {
        return (point - circleCenter).sqrMagnitude < radius * radius;
    }
    private bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        var s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
        var t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

        if ((s < 0) != (t < 0))
            return false;

        var A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
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

public class GeometryUtility : MonoBehaviour
{
    private Triangle[] triangles;
    private int layer = 0;
    private float scale = 1f;
    private float speed = 2f;
    private void Awake()
    {
        triangles = new Triangle[15];
    }
    public bool CanMove(Dictionary<int, Tile_t> map, Vector3 dir, out Vector3 goal)
    {
        Vector3 position = transform.position;
        dir *= Time.fixedDeltaTime * speed;

        goal = position + dir;
        if (scale * 0.25f > goal.x || scale * 0.25f > goal.z)
        {
            return false;
        }

        int keyMy = PTile.GetKey(layer, goal, scale);
        if (false == map.TryGetValue(keyMy, out Tile_t tileMy))
        {
            return false;
        }

        Vector3 pivot = PTile.GetPivot(goal, scale);
        int triangleTarget = PTile.GetTriangleIndex(goal - pivot, scale * 0.5f);
        float radius = tileMy.GetScale(TileSize.Quater);
        int index = 0;
        int keyTarget, shiftKey;

        switch (triangleTarget)
        {
            case 0:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 3, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 4, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);

                    if (true == tileMy.IsLinked(0, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                    }
                    if (true == tileMy.IsLinked(1, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 9, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                    if (true == tileMy.IsLinked(2, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 15, index);
                    }
                    if (true == tileMy.IsLinked(11, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                    }
                }
                break;
            case 1:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 0, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 3, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 4, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);

                    if (true == tileMy.IsLinked(1, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 9, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);
                    }
                    if (true == tileMy.IsLinked(2, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 15, index);
                    }
                }
                break;
            case 2:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 0, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 3, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 11, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);

                    if (true == tileMy.IsLinked(10, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 12, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                    }
                    if (true == tileMy.IsLinked(11, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 6, index);
                    }
                }
                break;
            case 3:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 0, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 3, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 11, goal, radius);

                    if (true == tileMy.IsLinked(0, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                    }
                    if (true == tileMy.IsLinked(1, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                    if (true == tileMy.IsLinked(10, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 12, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                    }
                    if (true == tileMy.IsLinked(11, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 6, index);
                    }
                }
                break;
            case 4:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 4, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 5, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 0, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);

                    if (true == tileMy.IsLinked(0, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 9, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);
                    }
                    if (true == tileMy.IsLinked(1, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 15, index);
                    }
                    if (true == tileMy.IsLinked(2, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                    }
                    if (true == tileMy.IsLinked(3, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                }
                break;
            case 5:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 4, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 5, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 13, goal, radius);

                    if (true == tileMy.IsLinked(2, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                    }
                    if (true == tileMy.IsLinked(3, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                    if (true == tileMy.IsLinked(4, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 2, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(5, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 8, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                }
                break;
            case 6:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 4, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 5, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 13, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);

                    if (true == tileMy.IsLinked(4, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 2, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(5, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 8, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                }
                break;
            case 7:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 4, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 5, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);

                    if (true == tileMy.IsLinked(1, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 9, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);

                    }
                    if (true == tileMy.IsLinked(2, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 15, index);
                    }
                }
                break;
            case 8:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 10, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 11, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 3, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);

                    if (true == tileMy.IsLinked(10, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 12, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                    }
                    if (true == tileMy.IsLinked(11, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 6, index);
                    }
                }
                break;
            case 9:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 10, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 11, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 14, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);

                    if (true == tileMy.IsLinked(7, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 7, index);
                    }
                    if (true == tileMy.IsLinked(8, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 1, index);
                    }
                }
                break;
            case 10:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 10, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 11, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 14, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);

                    if (true == tileMy.IsLinked(7, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 7, index);
                    }
                    if (true == tileMy.IsLinked(8, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 1, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(9, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                    }
                    if (true == tileMy.IsLinked(10, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 14, index);
                    }
                }
                break;
            case 11:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 10, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 11, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 3, goal, radius);

                    if (true == tileMy.IsLinked(8, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(9, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                    }
                    if (true == tileMy.IsLinked(10, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 12, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 13, index);
                    }
                    if (true == tileMy.IsLinked(11, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 6, index);
                    }
                }
                break;
            case 12:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 13, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 14, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 5, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);

                    if (true == tileMy.IsLinked(4, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 2, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(5, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 8, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                }
                break;
            case 13:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 13, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 14, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 5, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);

                    if (true == tileMy.IsLinked(4, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 2, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(5, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 8, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                    if (true == tileMy.IsLinked(6, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(7, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                    }
                }
                break;
            case 14:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 13, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 14, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 10, goal, radius);

                    if (true == tileMy.IsLinked(5, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 10, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 11, index);
                    }
                    if (true == tileMy.IsLinked(6, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 3, index);
                    }
                    if (true == tileMy.IsLinked(7, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 5, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 7, index);
                    }
                    if (true == tileMy.IsLinked(8, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 1, index);
                    }
                }
                break;
            case 15:
                {
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 12, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 13, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 14, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 15, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 6, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 7, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 1, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 2, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 8, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 9, goal, radius);
                    triangles[index++] = new Triangle(keyMy, tileMy, pivot, 10, goal, radius);

                    if (true == tileMy.IsLinked(7, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 4, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 7, index);
                    }
                    if (true == tileMy.IsLinked(8, out shiftKey))
                    {
                        keyTarget = keyMy + shiftKey;
                        index = AddLinked(map, triangles, keyTarget, goal, 0, index);
                        index = AddLinked(map, triangles, keyTarget, goal, 1, index);
                    }
                }
                break;
            default:
                return false;
        }

        bool canMove = false;
        for (int i = 0; i < index; ++i)
        {
            Triangle triangle = triangles[i];

            if (true == triangle.IsIntersected())
            {
                if (false == Dev_MapSampler.Map.ContainsKey(triangle.key))
                {
                    Debug.Log($"NULL_TILE: [{triangle.triangle}:{PTile.GetPivot(triangle.key, triangle.scale)}] {System.Convert.ToString(triangle.movement, 2)}");
                    goto CLOSE;
                }
                if (0 == (triangle.movement & (1 << triangle.triangle)))
                {
                    Debug.Log($"NOT_MOVE: [{triangle.triangle}:{PTile.GetPivot(triangle.key, triangle.scale)}] {System.Convert.ToString(triangle.movement, 2)}");
                    goto CLOSE;
                }
            }
        }

        canMove = true;
        goal = CMathf.CMath.FloorToVector(goal, 3);

    CLOSE:
        //어차피 index, length를 매번 갱신하니 Clear 할 필요도 없음. (포인터스럽게..)
        return canMove;
    }
    private int AddLinked(Dictionary<int, Tile_t> map, Triangle[] array, int key, Vector3 center, int triangle, int index)
    {
        for (int y = 1; y >= -1; --y)
        {
            int keyTarget = key + y * (1 << 8);
            Vector3 pivot = PTile.GetPivot(keyTarget, scale);
            if (true == map.TryGetValue(keyTarget, out Tile_t tileTarget))
            {
                float radius = tileTarget.GetScale(TileSize.Quater);
                array[index++] = new Triangle(keyTarget, tileTarget, pivot, triangle, center, radius);
                break;
            }
        }

        return index;
    }
}
