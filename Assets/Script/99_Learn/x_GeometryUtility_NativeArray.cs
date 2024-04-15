//using UnityEngine;
//using Unity.Jobs;
//using Unity.Burst;
//using Unity.Collections;
//using System.Collections.Generic;
//using DataType;

//public struct x_IsPossibleToMove
//{
//    //public Vector3 pivot;
//    public int key;
//    public byte triangle;
//    public short movement;
//    public float scale;

//    public Vector3 position;
//    public float radius;
//}
//[BurstCompile]
//public struct x_CheckTriangleCollsion : IJobParallelFor
//{
//    [ReadOnly] public NativeArray<x_IsPossibleToMove> Targets;
//    [WriteOnly] public NativeArray<bool> Collision;

//    public void Execute(int index)
//    {
//        x_IsPossibleToMove data = Targets[index];

//        bool result = true; // 상태값 (not_collided, movable, not_movable이 더 정확한 표현이긴 함;)
//        if (true == IsCircleTriangleIntersect(data))
//        {
//            result = (0 != (data.movement & (1 << data.triangle)));
//        }

//        Collision[index] = result;
//    }
//    private bool IsCircleTriangleIntersect(x_IsPossibleToMove data)
//    {
//        Vector3 circleCenter = data.position;
//        float radius = data.radius;
//        float size = Index.IDxTile.SIZE_QUATER * data.scale;

//        Vector3 pivot = PTile.GetPivot(data.key, data.scale);
//        Vector3 A = pivot, B = pivot, C = pivot;

//        switch (data.triangle % 4)
//        {
//            case 0:
//                A += new Vector3(0.25f, 0f, 0.25f) * size;
//                B += new Vector3(0.5f, 0f, 0f) * size;

//                break;
//            case 1:
//                A += new Vector3(0.25f, 0f, 0.25f) * size;
//                B += new Vector3(0.5f, 0, 0) * size;
//                C += new Vector3(0.5f, 0, 0.5f) * size;
//                break;
//            case 2:
//                A += new Vector3(0.25f, 0f, 0.25f) * size;
//                B += new Vector3(0, 0, 0.5f) * size;
//                C += new Vector3(0.5f, 0, 0.5f) * size;
//                break;
//            case 3:
//                A += new Vector3(0.25f, 0f, 0.25f) * size;
//                B += new Vector3(0, 0, 0.5f);

//                break;
//            default:
//                return false;
//        }

//        switch ((int)(data.triangle * 0.25f))
//        {
//            case 1:
//                A += new Vector3(0.5f, 0, 0) * size;
//                B += new Vector3(0.5f, 0, 0) * size;
//                C += new Vector3(0.5f, 0, 0) * size;
//                break;
//            case 2:
//                A += new Vector3(0, 0, 0.5f) * size;
//                B += new Vector3(0, 0, 0.5f) * size;
//                C += new Vector3(0, 0, 0.5f) * size;
//                break;
//            case 3:
//                A += new Vector3(0.5f, 0, 0.5f) * size;
//                B += new Vector3(0.5f, 0, 0.5f) * size;
//                C += new Vector3(0.5f, 0, 0.5f) * size;
//                break;
//        }

//        // 원의 중심이 삼각형 내부에 있는지 확인
//        if (PointInTriangle(circleCenter, A, B, C))
//            return true;

//        // 삼각형의 각 꼭짓점이 원 내부에 있는지 확인
//        if (IsPointInsideCircle(A, circleCenter, radius) ||
//            IsPointInsideCircle(B, circleCenter, radius) ||
//            IsPointInsideCircle(C, circleCenter, radius))
//            return true;

//        // 삼각형의 각 변과 원의 교차 확인
//        if (IsCircleLineIntersect(circleCenter, radius, A, B) ||
//            IsCircleLineIntersect(circleCenter, radius, B, C) ||
//            IsCircleLineIntersect(circleCenter, radius, C, A))
//            return true;

//        return false;
//    }
//    private bool IsPointInsideCircle(Vector2 point, Vector2 circleCenter, float radius)
//    {
//        return (point - circleCenter).sqrMagnitude < radius * radius;
//    }
//    private bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
//    {
//        var s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
//        var t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

//        if ((s < 0) != (t < 0))
//            return false;

//        var A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
//        if (A < 0.0)
//        {
//            s = -s;
//            t = -t;
//            A = -A;
//        }
//        return s > 0 && t > 0 && (s + t) < A;
//    }
//    private bool IsCircleLineIntersect(Vector2 circleCenter, float radius, Vector2 A, Vector2 B)
//    {
//        // 선분 AB의 방향 벡터를 계산합니다.
//        Vector2 d = B - A;

//        // 원의 중심에서 점 A까지의 벡터를 계산합니다.
//        Vector2 f = A - circleCenter;

//        // 2차 방정식의 계수 a, b, c를 계산합니다. 이 방정식은 선분과 원의 교차 조건을 나타냅니다.
//        float a = Vector2.Dot(d, d); // d 벡터의 길이의 제곱
//        float b = 2 * Vector2.Dot(f, d); // f와 d 벡터의 내적을 2배 한 값
//        float c = Vector2.Dot(f, f) - radius * radius; // f 벡터의 길이의 제곱에서 원의 반지름 제곱을 뺀 값

//        // 판별식을 계산합니다. 이 값이 양수라면 근이 실수로 존재함을 의미합니다.
//        float discriminant = b * b - 4 * a * c;

//        if (discriminant < 0)
//        {
//            // 판별식이 음수이면, 선분과 원은 서로 교차하지 않습니다.
//            return false;
//        }
//        else
//        {
//            // 판별식의 제곱근을 구하여 실제 근을 찾습니다.
//            discriminant = Mathf.Sqrt(discriminant);

//            // 근의 공식을 사용하여 두 근을 계산합니다.
//            float t1 = (-b - discriminant) / (2 * a);
//            float t2 = (-b + discriminant) / (2 * a);

//            // 두 근 중 하나라도 선분의 파라미터 0과 1 사이에 있으면, 선분이 원과 교차합니다.
//            if (t1 >= 0 && t1 <= 1 || t2 >= 0 && t2 <= 1)
//                return true;

//            // 그렇지 않다면 교차하지 않습니다.
//            return false;
//        }
//    }
//}

//public class x_GeometryUtility_NativeArray : MonoBehaviour
//{
//    private List<Triangle> triangles = new List<Triangle>();
//    private NativeArray<x_IsPossibleToMove> targets;
//    private NativeArray<bool> isCollided;
//    private int layer = 0;
//    private float scale = 1f;
//    private float speed = 2f;

//    public bool CanMove(Dictionary<int, Tile_t> map, Vector3 dir, out Vector3 goal)
//    {
//        int length;
//        bool canMove = false;
//        Vector3 position = transform.position;
//        dir *= Time.fixedDeltaTime * speed;

//        //��ǥ ��ġ�� ���� ���
//        goal = position + dir;

//        //���⼭ is in grid ��� �� ���� ��.
//        if (scale * 0.25f > goal.x || scale * 0.25f > goal.z)
//        {
//            return false;
//        }

//        Vector3 pivot = PTile.GetPivot(goal, scale);
//        Tile_t tileMy = map[PTile.GetKey(layer, goal, scale)];
//        int keyMy = PTile.GetKey(layer, goal, scale);
//        int indexTriangle = PTile.GetTriangleIndex(goal - pivot, scale * 0.5f);

//        //get triangle index
//        byte index;
//        short move;
//        float radius;

//        length = 15;
//        targets = new NativeArray<x_IsPossibleToMove>(length, Allocator.TempJob);
//        isCollided = new NativeArray<bool>(length, Allocator.TempJob);

//        int keyTarget, shiftKey;

//        //��¼�� �̷��� �Ǿ��°���..
//        //������ ������尡 �ɸ��� �ִ� �� ����? �������� �̷��� �������ٴ�
//        switch (indexTriangle)
//        {
//            case 0:
//                {
//                    //���� Ÿ��
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);

//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 0, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 1, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 2, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 3, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 4, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 7, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(0, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 13, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 14, index);
//                    }
//                    if (true == tileMy.IsLinked(1, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 9, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 10, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 11, index);
//                    }
//                    if (true == tileMy.IsLinked(2, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 14, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 15, index);
//                    }
//                    if (true == tileMy.IsLinked(11, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 4, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 5, index);
//                    }

//                    length = index;
//                }
//                break;
//            case 1:
//                {
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 0, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 1, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 2, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 3, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 4, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 6, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 7, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 8, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 9, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 12, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 15, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(1, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 9, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 10, index);
//                    }
//                    if (true == tileMy.IsLinked(2, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 14, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 15, index);
//                    }
//                    length = index;
//                }
//                break;
//            case 2:
//                {
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 0, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 1, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 2, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 3, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 6, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 7, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 8, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 9, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 11, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 12, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 15, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(10, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 12, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 13, index);
//                    }
//                    if (true == tileMy.IsLinked(11, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 5, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 6, index);
//                    }
//                    length = index;
//                }
//                break;
//            case 3:
//                {
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 0, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 1, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 2, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 3, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 8, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 11, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(0, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 13, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 14, index);
//                    }
//                    if (true == tileMy.IsLinked(1, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 10, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 11, index);
//                    }
//                    if (true == tileMy.IsLinked(10, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 12, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 13, index);
//                    }
//                    if (true == tileMy.IsLinked(11, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 4, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 5, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 6, index);
//                    }

//                    length = index;
//                }
//                break;
//            case 4:
//                {
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);

//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 4, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 5, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 6, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 7, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 0, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 1, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(0, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 9, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 10, index);
//                    }

//                    if (true == tileMy.IsLinked(1, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 13, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 14, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 15, index);
//                    }

//                    if (true == tileMy.IsLinked(2, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 10, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 11, index);
//                    }

//                    if (true == tileMy.IsLinked(3, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 0, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 3, index);
//                    }

//                    length = index;
//                }
//                break;
//            case 5:
//                {
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 4, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 5, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 6, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 7, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 12, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 13, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(2, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 13, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 14, index);
//                    }
//                    if (true == tileMy.IsLinked(3, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 10, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 11, index);
//                    }
//                    if (true == tileMy.IsLinked(4, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 0, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 2, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 3, index);
//                    }
//                    if (true == tileMy.IsLinked(5, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 8, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 11, index);
//                    }

//                    length = index;
//                }
//                break;
//            case 6:
//                {
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 4, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 5, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 6, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 7, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 1, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 2, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 8, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 9, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 12, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 13, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 15, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(4, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 2, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 3, index);
//                    }
//                    if (true == tileMy.IsLinked(5, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 8, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 11, index);
//                    }
//                    length = index;
//                }
//                break;
//            case 7:
//                {
//                    move = (short)tileMy.Move;
//                    index = 0;
//                    radius = tileMy.GetScale(TileSize.Quater);
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 4, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 5, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 6, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 7, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 0, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 1, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 2, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 8, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 9, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 12, position = position, scale = scale, radius = radius };
//                    targets[index++] = new x_IsPossibleToMove { key = keyMy, movement = move, triangle = 15, position = position, scale = scale, radius = radius };

//                    if (true == tileMy.IsLinked(1, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 9, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 10, index);
//                    }
//                    if (true == tileMy.IsLinked(2, out shiftKey))
//                    {
//                        keyTarget = keyMy + shiftKey;
//                        index = SetCheckCollisionJob(map, keyTarget, position, 14, index);
//                        index = SetCheckCollisionJob(map, keyTarget, position, 15, index);
//                    }

//                    length = index;
//                }
//                break;
//            default:
//                canMove = false;
//                goto DISPOSE;
//        }

//        x_CheckTriangleCollsion job = new x_CheckTriangleCollsion
//        {
//            Targets = targets,
//            Collision = isCollided
//        };
//        JobHandle handle = job.Schedule(length, length);
//        handle.Complete();

//        for (int i = 0; i < length; i++)
//        {
//            //is collided
//            if (true == isCollided[i])
//            {
//                x_IsPossibleToMove data = targets[i];

//                if (false == Dev_MapSampler.Map.ContainsKey(data.key))
//                {
//                    Debug.Log($"NULL_TILE: [{data.triangle}:{PTile.GetPivot(data.key, data.scale)}] {System.Convert.ToString(data.movement, 2)}");
//                    goto DISPOSE;
//                }
//                if (0 == (data.movement & (1 << data.triangle)))
//                {
//                    Debug.Log($"NOT_MOVE: [{data.triangle}:{PTile.GetPivot(data.key, data.scale)}] {System.Convert.ToString(data.movement, 2)}");
//                    goto DISPOSE;
//                }
//            }
//        }

//        //TODO: ���⼭ y�൵ ì�ܾ� �Ѵ�.
//        canMove = true;

//    DISPOSE:
//        targets.Dispose();
//        isCollided.Dispose();

//        Debug.Log("Result: " + canMove);
//        return canMove;
//    }
//    private byte SetCheckCollisionJob(Dictionary<int, Tile_t> map, int key, Vector3 center, byte triangle, byte indexJob)
//    {
//        short move;
//        float radius;

//        for (int y = 1; y >= -1; --y)
//        {
//            int keyTarget = key + y * (1 << 8);
//            if (true == map.TryGetValue(keyTarget, out Tile_t tileTarget))
//            {
//                move = (short)tileTarget.Move;
//                radius = tileTarget.GetScale(TileSize.Quater);
//                targets[indexJob++] = new x_IsPossibleToMove { key = keyTarget, movement = move, triangle = triangle, position = center, radius = radius };
//                break;
//            }
//        }

//        return indexJob;
//    }
//}
