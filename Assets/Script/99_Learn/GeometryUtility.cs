using UnityEngine;
using System.Collections.Generic;
using DataType;
using UnityEngine.UIElements;
using System;

public struct TriangleCollision
{
    public Vector2 A, B, C; // x,z로만 판별
    public int key;
    public int index;

    public TriangleCollision(int key, Vector3 pivot, int indexTriangle, float scale)
    {
        this.key = key;
        this.index = indexTriangle;

        Vector2 pivot2d = new Vector2(pivot.x, pivot.z);
        A = pivot2d;
        B = pivot2d;
        C = pivot2d;

        float scale_quater = scale * 0.25f;
        switch (index % 4)
        {
            case 0:
                A += new Vector2(1f, 1f) * scale_quater;
                B += new Vector2(2f, 0f) * scale_quater;

                break;
            case 1:
                A += new Vector2(1f, 1f) * scale_quater;
                B += new Vector2(2f, 0)  * scale_quater;
                C += new Vector2(2f, 2f) * scale_quater;
                break;
            case 2:
                A += new Vector2(1f, 1f) * scale_quater;
                B += new Vector2(0, 2f)  * scale_quater;
                C += new Vector2(2f, 2f) * scale_quater;
                break;
            case 3:
                A += new Vector2(1f, 1f) * scale_quater;
                B += new Vector2(0, 2f)  * scale_quater;

                break;
        }

        switch ((int)(index * 0.25f))
        {
            case 1:
                A += new Vector2(2f, 0) * scale_quater;
                B += new Vector2(2f, 0) * scale_quater;
                C += new Vector2(2f, 0) * scale_quater;
                break;
            case 2:
                A += new Vector2(0, 2f) * scale_quater;
                B += new Vector2(0, 2f) * scale_quater;
                C += new Vector2(0, 2f) * scale_quater;
                break;
            case 3:
                A += new Vector2(2f, 2f) * scale_quater;
                B += new Vector2(2f, 2f) * scale_quater;
                C += new Vector2(2f, 2f) * scale_quater;
                break;
        }
    }

    public bool IsIntersected(Vector3 center, float radius)
    {
        Vector2 center2D = new Vector2(center.x, center.z);

        if (PointInTriangle(center2D, A, B, C))
        {
            return true;
        }

        // 삼각형의 각 꼭짓점이 원 내부에 있는지 확인
        if (IsPointInsideCircle(A, center2D, radius) ||
            IsPointInsideCircle(B, center2D, radius) ||
            IsPointInsideCircle(C, center2D, radius))
        {
            return true;
        }


        // 삼각형의 각 변과 원의 교차 확인
        if (IsCircleLineIntersect(center2D, radius, A, B) ||
            IsCircleLineIntersect(center2D, radius, B, C) ||
            IsCircleLineIntersect(center2D, radius, C, A))
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

public class GeometryUtility : MonoBehaviour
{
    private TriangleCollision[] triangles;
    private int   layer;
    private float scale;
    private float speed;
    private void Awake()
    {
        triangles = new TriangleCollision[15];
        layer = 0;
        scale = 1f;
        speed = 2f;
    }
    public bool CanMove(Dictionary<int, Tile_t> map, Vector3 dir, out Vector3 goal)
    {
        dir *= Time.fixedDeltaTime * speed;
        goal = transform.position + dir;

        int keyMy = PTile.GetKey(layer, goal, scale);
        keyMy = PTile.GetKey_FromRelativeCoord(map, keyMy, 0, 0);
        if (-1 == keyMy)
        {
            //목적 지점에서 tile_t 정보를 찾을 수 없다면 return false;
            return false;
        }

        Vector3 pivot = PTile.GetPivot(goal, scale);
        Vector3 pivotNeighbor;
        int triangleTarget = PTile.GetTriangleIndex(goal - pivot, scale * 0.5f);
        int index = 0;
        bool canMove = false;

        switch (triangleTarget)
        {
            case 0:
                //params[] 쓰면 편할 텐데 힙 메모리는 가능하면 지양하기로.
                triangles[index++] = new TriangleCollision(keyMy, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 7, scale);

                //neighbor: z-1
                int keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

                //neighbor: x-1, z-1
                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: -1);
                pivotNeighbor = pivot + new Vector3(-1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                //neighbor: x-1
                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                break;

            case 1:
                triangles[index++] = new TriangleCollision(keyMy, pivot,  0, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  3, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  4, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: -1);
                pivotNeighbor = pivot + new Vector3(+1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);
                break;

            case 2:
                triangles[index++] = new TriangleCollision(keyMy, pivot,  0, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  3, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                break;

            case 3:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 11, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);

                break;
            case 4:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 0, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 1, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: -1);
                pivotNeighbor = pivot + new Vector3(1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                break;
            case 5:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 13, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: -1);
                pivotNeighbor = pivot + new Vector3(+1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                break;

            case 6:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 4, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                break;
            case 7:
                triangles[index++] = new TriangleCollision(keyMy, pivot,  4, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  5, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);
                break;
            case 8:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 3, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                break;
            case 9:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
                break;
            case 10:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: +1);
                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                break;
            case 11:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 10, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 11, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 3, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: +1);
                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
                pivotNeighbor = pivot + new Vector3(-1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 6, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 12, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                break;
            case 12:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  5, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot,  7, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor,  2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor,  3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor,  8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                break;
            case 13:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 5, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 6, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: +1);
                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                break;
            case 14:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 10, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: +1);
                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
                break;
            case 15:
                triangles[index++] = new TriangleCollision(keyMy, pivot, 12, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 13, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 14, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 15, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 6, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 7, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 1, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 2, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 8, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 9, scale);
                triangles[index++] = new TriangleCollision(keyMy, pivot, 10, scale);

                keyLink = PTile.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);
                break;
            default:
                return false;
        }

        float dist = scale * Index.IDxTile.SIZE_QUATER - Time.fixedDeltaTime;
        for (int i = 0; i < index; ++i)
        {
            TriangleCollision triangle = triangles[i];
            if (true == triangle.IsIntersected(goal, dist))
            {
                if (false == Dev_MapSampler.Map.TryGetValue(triangle.key, out Tile_t tileChecked))
                {
                    goto CLOSE;
                }
                if (false == tileChecked.IsMovable(triangle.index))
                {
                    goto CLOSE;
                }
            }
        }

        if (true == map.TryGetValue(keyMy, out Tile_t tileMy))
        {
            float y = tileMy.GetYValue(keyMy, goal);
            goal = CMathf.CMath.FloorToVector(new Vector3(goal.x, y, goal.z), 3);
            //goal = Vector3.Lerp(transform.position, new Vector3(goal.x, y, goal.z), 0.5f);
            //goal = CMathf.CMath.FloorToVector(goal, 3);
            canMove = true;
        }

    CLOSE:
        //어차피 index, length를 매번 갱신하니 Clear 할 필요도 없음. (포인터스럽게..)
        return canMove;
    }
}
