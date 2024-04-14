using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

public struct Circle
{
    public Vector2 Center;
    public float Radius;
}

public struct Triangle
{
    public Vector2 VertexA;
    public Vector2 VertexB;
    public Vector2 VertexC;
}
[BurstCompile]
public struct CircleTriangleIntersectionJob : IJobParallelFor
{
    public Circle Circle;
    [ReadOnly] public NativeArray<Triangle> Triangles;
    [WriteOnly] public NativeArray<bool> Results;

    public void Execute(int index)
    {
        Triangle triangle = Triangles[index];
        Results[index] = IsCircleTriangleIntersect(Circle, triangle);
    }

    private bool IsCircleTriangleIntersect(Circle circle, Triangle triangle)
    {
        Vector3 circleCenter = circle.Center;
        float radius = circle.Radius;

        Vector3 A = triangle.VertexA;
        Vector3 B = triangle.VertexB;
        Vector3 C = triangle.VertexC;


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


public class GeometryUtility : MonoBehaviour
{

    NativeArray<Triangle> triangles;
    NativeArray<bool> results;
    int numTriangles = 16; // 예시로 16개의 삼각형

    private void Awake()
    {
        triangles = new NativeArray<Triangle>(numTriangles, Allocator.TempJob);
        results = new NativeArray<bool>(numTriangles, Allocator.TempJob);
    }
    public void CheckIntersections()
    {
        for (int i = 0; i < triangles.Length; i++)
        {
            // 임의의 삼각형 데이터를 생성하여 할당
            triangles[i] = new Triangle
            {
                VertexA = new Vector2(Random.value, Random.value),
                VertexB = new Vector2(Random.value, Random.value),
                VertexC = new Vector2(Random.value, Random.value)
            };
            Debug.Log($"[{i}] {triangles[i].VertexA},{triangles[i].VertexB},{triangles[i].VertexC}");
        }
        Circle circle = new Circle { Center = new Vector2(1, 1), Radius = 0.5f };
        CircleTriangleIntersectionJob job = new CircleTriangleIntersectionJob
        {
            Circle = circle,
            Triangles = triangles,
            Results = results
        };
        JobHandle handle = job.Schedule(numTriangles, 4); // 4개의 삼각형 단위로 분할하여 처리
        handle.Complete();

        // 결과 처리
        for (int i = 0; i < numTriangles; i++)
        {
            if (results[i])
            {
                Debug.Log("Intersection found at triangle: " + i);
            }
        }

        triangles.Dispose();
        results.Dispose();
    }

    private void Start()
    {
        CheckIntersections();
    }
}
