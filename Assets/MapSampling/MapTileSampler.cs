using CMathf;
using UnityEngine;
using DataStruct;

public class MapTileSampler : MonoBehaviour
{
    //[SerializeField]
    //private ETileTriggerFlag triggerType; // : byte
    //[SerializeField]
    //private int triggerValue;

    [SerializeField]
    private bool isHalfScale;
    [SerializeField]
    private int meshLayer;

    private MeshFilter _meshFilter;

    private Vector3 _gridPivot;
    private Vector3 _tilePivot;
    private short _gridIndexFlag;
    private short _tileIndexFlag;
    private short _tileInfoFlag;
    private long _collisionFlag;

    /* grid info */
    public Vector3 GridPivot => _gridPivot;
    public short GridIndexFlag => _gridIndexFlag;

    /* tile info */
    public Vector3 TilePivot => _tilePivot;
    public short IndexFlag => _tileIndexFlag;
    public short InfoFlag => _tileInfoFlag;
    public long CollisionFlag => _collisionFlag;
    public bool IsHalfScale => isHalfScale;
    public MeshFilter MeshFilter => _meshFilter;

    [HideInInspector] public bool IsHalf => isHalfScale;
    
    public void Init()
    {
        /* mesh */
        _meshFilter = transform.GetComponent<MeshFilter>();

        /* transform => tile pivot*/
        _tilePivot = GetTilePivotFromCenter(transform.position);

        /* grid index flag */
        var gridX = Mathf.FloorToInt(_tilePivot.x / 32);
        var gridY = Mathf.FloorToInt(_tilePivot.y / 4);
        var gridZ = Mathf.FloorToInt(_tilePivot.z / 32);
        _gridIndexFlag = GetGridIndexFlag(gridX, gridY, gridZ);

        _gridPivot = new Vector3(gridX * 32, gridY * 4, gridZ * 32).Truncate();
        var diffInt = (_tilePivot - _gridPivot).ToInt();

        _tileIndexFlag = GetTileIndexFlag(diffInt);
        _tileInfoFlag = GetTileInfoFlag();
        _collisionFlag = GetCollideFlag(_tilePivot);

        /* for debug */
        //debug_gridIndex = gridIndexFlag;
        //debug_gridPivot = gridPivot;
        //debug_data = new MapTileData(tileIndexFlag, tileInfoFlag, collisionFlag);
    }



    public (long, MapTileData) Set()
    {
        short gridIndex;
        short tileIndexFlag;
        short tileInfo;
        long collide;

        /* transform => tile pivot*/
        Vector3 tilePivot = GetTilePivotFromCenter(transform.position);

        /* grid index flag */
        int grid_x = Mathf.FloorToInt(tilePivot.x / 32);
        int grid_y = Mathf.FloorToInt(tilePivot.y /  4);
        int grid_z = Mathf.FloorToInt(tilePivot.z / 32);
        gridIndex  = GetGridIndexFlag(grid_x, grid_y, grid_z);


        /* tile index flag */
        Vector3 grid_pivot = new Vector3(grid_x * 32, grid_y * 4, grid_z * 32).Truncate();
        Vector3Int diffInt  = (tilePivot - grid_pivot).ToInt();
        tileIndexFlag = GetTileIndexFlag(diffInt);

        /* tile info */
        tileInfo = GetTileInfoFlag();

        /* collide */
        collide = GetCollideFlag(tilePivot);

        /* for debug */
        //debug_gridIndex = gridIndex;
        //debug_gridPivot = grid_pivot;
        //debug_data = new MapTileData(tileIndexFlag, tileInfo, collide);

        return ((long)gridIndex << 16 | (long)tileIndexFlag, new MapTileData(tileIndexFlag, tileInfo, collide));
    }

    private Vector3 GetTilePivotFromCenter(Vector3 center)
    {
        center.Truncate();

        float scale = isHalfScale ? 0.5f : 1f;
        float sign_x = (center.x < 0) ? -1 : 1;
        float sign_y = (center.y < 0) ? -1 : 1;
        float sign_z = (center.z < 0) ? -1 : 1;

        float tile_x = center.x - (sign_x * (center.x % scale));
        float tile_y = center.y - (sign_y * (center.y % scale));
        float tile_z = center.z - (sign_z * (center.z % scale));

        return new Vector3(tile_x, tile_y, tile_z).Truncate();
    }
    private short GetGridIndexFlag(int pointX, int pointY, int pointZ)
    {
        int shiftGridXSign = 15;
        int shiftGridX     = 10;
        int shiftGridYSign =  9;
        int shiftGridY     =  6;
        int shiftGridZSign =  5;
        int shiftGridZ     =  0;

        int gridFlag = 0;

        if (pointX < 0)
        {
            gridFlag |= 1 << shiftGridXSign;
            gridFlag |= (-pointX) << shiftGridX;
        }
        else
        {
            gridFlag |= pointX << shiftGridX;
        }

        if (pointY < 0)
        {
            gridFlag |= 1 << shiftGridYSign;
            gridFlag |= (-pointY) << shiftGridY;
        }
        else
        {
            gridFlag |= pointY << shiftGridY;
        }

        if (pointZ < 0)
        {
            gridFlag |= 1 << shiftGridZSign;
            gridFlag |= (-pointZ) << shiftGridZ;
        }
        else
        {
            gridFlag |= pointZ << shiftGridZ;
        }

        return (short)gridFlag;
    }
    private short GetTileIndexFlag(Vector3Int diffInt)
    {
        var shiftIsHalfScale = 15;
        var shiftTileX       =  9;
        var shiftTileY       =  6;
        var shiftTileZ       =  0;

        int tileFlag = 0;
        tileFlag |= (isHalfScale == true) ? 1 << shiftIsHalfScale : 0;
        tileFlag |= (diffInt.x) << shiftTileX;
        tileFlag |= (diffInt.y) << shiftTileY;
        tileFlag |= (diffInt.z) << shiftTileZ;

        return (short)tileFlag;
    }
    private short GetTileInfoFlag()
    {
        var shiftMeshLayer    = 13;
        //int shiftTriggerType  =  9;
        //int shiftTriggerValue =  0;

        var infoFlag = 0;
        infoFlag |= meshLayer << shiftMeshLayer;
        // 차라리 필드의 속성(ex. 비, 눈, 진흙, .. 을 넣는게 좋겠다.)
        //infoFlag |= (int)triggerType << shiftTriggerType;
        //infoFlag |= triggerValue << shiftTriggerValue;

        return (short)infoFlag;
    }
    private long GetCollideFlag(Vector3 tilePivot)
    {
        long collide = 0;
        
        var mesh = _meshFilter.sharedMesh;
        var rot = transform.rotation;
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var triangles = mesh.triangles;

        for (var t = 0; t < triangles.Length; t += 3)
        {
            var t0 = triangles[t];
            var t1 = triangles[t + 1];
            var t2 = triangles[t + 2];

            //Determine whether the mesh is the target for sampling by normal value.
            var normal1 = rot * normals[t0];
            var normal2 = rot * normals[t1];
            var normal3 = rot * normals[t2];
            var normal = normal1;
            if (normal2.y < normal.y) { normal = normal2; }
            if (normal3.y < normal.y) { normal = normal3; }
            normal = normal.Truncate();

            if (0 >= normal.y)
            {
                continue;
            }

            var margin = 0.125f;
            var a = GetSnappingPoint(transform.TransformPoint(vertices[t0]), margin);
            var b = GetSnappingPoint(transform.TransformPoint(vertices[t1]), margin);
            var c = GetSnappingPoint(transform.TransformPoint(vertices[t2]), margin);

            var scale = isHalfScale ? 0.5f : 1f;
            collide |= GetTileDataRecursive(0, tilePivot, a, b, c, scale);
        }

        return collide;
    }
    private Vector3 GetSnappingPoint(Vector3 p, float margin)
    {
        var x = p.x;
        var y = p.y;
        var z = p.z;

        //Similar to rounding, but the standard is different for each dist, not 0.5f.
        var diff = x % margin;
        if (0 < diff & diff <= margin * 0.1f)
        {
            x -= diff;
        }
        else if (margin * 0.9f <= diff && diff < margin)
        {
            x += (margin - diff);
        }

        diff = y % margin;
        if (0 < diff & diff <= margin * 0.1f)
        {
            y -= diff;
        }
        else if (margin * 0.9f <= diff && diff < margin)
        {
            y += (margin - diff);
        }

        diff = z % margin;
        if (0 < diff & diff <= margin * 0.1f)
        {
            z -= diff;
        }
        else if (margin * 0.9f <= diff && diff < margin)
        {
            z += (margin - diff);
        }

        return new Vector3(x, y, z).Truncate();
    }
    private long GetTileDataRecursive(long collide, Vector3 pivot, Vector3 p0, Vector3 p1, Vector3 p2, float scale)
    {
        var v0To1 = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p1.x, 0, p1.z)).Truncate();
        var v1To2 = Vector3.Distance(new Vector3(p1.x, 0, p1.z), new Vector3(p2.x, 0, p2.z)).Truncate();
        var v0To2 = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p2.x, 0, p2.z)).Truncate();

        var diagonal = v1To2;
        Vector3 swap;

        //빠른 탐색을 위하여 꼭지점의 각이 직각인 점을 v0로 설정한다. (모든 삼각형이 직각 이등변 삼각형이라 가능함.)
        if (diagonal < v0To1)
        {
            swap = p2;
            p2 = p0;
            p0 = swap;

            diagonal = v0To1;
        }
        if (diagonal < v0To2)
        {
            swap = p1;
            p1 = p0;
            p0 = swap;

            diagonal = v0To2;
        }

        var scaleHalf   = scale * 0.5f;
        var scaleQuater = scale * 0.25f;

        //삼각형 중 가장 긴 변이 단위 길이(scale_half)보다 같거나 짧을 때까지 재귀호출
        if (scaleHalf < diagonal)
        {
            var midPoint = ((p1 + p2) * 0.5f).Truncate();
            collide |= GetTileDataRecursive(collide, pivot, p0, p1, midPoint, scale);
            collide |= GetTileDataRecursive(collide, pivot, p0, p2, midPoint, scale);
        }
        else
        {
            //get point, get pivot
            var pointCenter = GetSnappingPoint((p0 + p1 + p2) * 0.333f, scaleQuater/* * 0.5f*/);

            //set flag
            long movable = 1 << TileUtility.GetTriangleIndex((pointCenter - pivot).Truncate(), scaleHalf);
            collide |= movable << (13 * 3);

            long height = 0;

            float scaleQuaterInverse = (1 / scaleQuater).Truncate();
            height |= GetHeightFlag(p0 - pivot, scaleQuaterInverse);
            height |= GetHeightFlag(p1 - pivot, scaleQuaterInverse);
            height |= GetHeightFlag(p2 - pivot, scaleQuaterInverse);
            collide |= height;
        }

        return collide;
    }
    private long GetHeightFlag(Vector3 diff, float scaleQuaterInverse)
    {
        diff = diff.Truncate();
        var x = (int) (diff.x * scaleQuaterInverse);
        var y = (long)(diff.y * scaleQuaterInverse);  //y: 0 ~ 4 (0b000 ~ 0b100)
        var z = (int) (diff.z * scaleQuaterInverse);

        int shift;
        switch (x * 10 + z)
        {
            case 00: shift = 0; break;
            case 20: shift = 1; break;
            case 40: shift = 2; break;
            case 02: shift = 3; break;
            case 22: shift = 4; break;
            case 42: shift = 5; break;
            case 04: shift = 6; break;
            case 24: shift = 7; break;
            case 44: shift = 8; break;
            case 11: shift = 9; break;
            case 31: shift = 10; break;
            case 13: shift = 11; break;
            case 33: shift = 12; break;
            default:
                Debug.LogError($"{diff:F3} {x},{z} => {y}");
                return 0;
        }
        shift *= 3;

        return y << shift;
    }

    public static MapTileSampler operator |(MapTileSampler a, MapTileSampler b)
    {
        if (a._gridIndexFlag != b._gridIndexFlag)
        {
            return a;
        }

        if (a._tileIndexFlag != b._tileIndexFlag)
        {
            return a;
        }

        a._tileInfoFlag  |= b._tileInfoFlag;
        a._collisionFlag |= b._collisionFlag;
        return a;
    }
}
