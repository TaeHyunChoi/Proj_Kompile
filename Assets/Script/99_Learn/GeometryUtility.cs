using UnityEngine;
using System.Collections.Generic;
using DataType;
using UnityEngine.UIElements;
using System;

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

        int keyMy = TileUtility.GetKey(layer, goal, scale);
        keyMy = TileUtility.GetKey_FromRelativeCoord(map, keyMy, 0, 0);
        if (-1 == keyMy)
        {
            //목적 지점에서 tile_t 정보를 찾을 수 없다면 return false;
            return false;
        }

        Vector3 pivot = TileUtility.GetPivot(goal, scale);
        Vector3 pivotNeighbor;
        int triangleTarget = TileUtility.GetTriangleIndex(goal - pivot, scale * 0.5f);
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
                int keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

                //neighbor: x-1, z-1
                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: -1);
                pivotNeighbor = pivot + new Vector3(-1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                //neighbor: x-1
                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: -1);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 9, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 15, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: -1);
                pivotNeighbor = pivot + new Vector3(1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
                pivotNeighbor = pivot + new Vector3(0, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 13, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 14, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: -1);
                pivotNeighbor = pivot + new Vector3(+1, 0, -1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: -1);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 1, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 7, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: +1);
                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
                pivotNeighbor = pivot + new Vector3(0, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: +1);
                pivotNeighbor = pivot + new Vector3(-1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 4, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 5, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: -1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 2, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 8, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: +1);
                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: 0);
                pivotNeighbor = pivot + new Vector3(+1, 0, 0) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 10, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 11, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: +1, z: +1);
                pivotNeighbor = pivot + new Vector3(+1, 0, +1) * scale;
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 0, scale);
                triangles[index++] = new TriangleCollision(keyLink, pivotNeighbor, 3, scale);

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
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

                keyLink = TileUtility.GetKey_FromRelativeCoord(map, keyMy, x: 0, z: +1);
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

            //height point를 어찌 비교하면 좋을까? 그냥 이전거 그대로 쓸까.. 흠...


            canMove = true;
        }

    CLOSE:
        //어차피 index, length를 매번 갱신하니 Clear 할 필요도 없음. (포인터스럽게..)
        return canMove;
    }
}
