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

    private MeshFilter meshFilter;

    private Vector3 gridPivot;
    private Vector3 tilePivot;
    private short gridIndexFlag;
    private short tileIndexFlag;
    private short tileInfoFlag;
    private long collisionFlag;

    /* grid info */
    public Vector3 GridPivot      { get { return gridPivot; } }
    public short GridIndexFlag    { get { return gridIndexFlag; } }

    /* tile info */
    public Vector3 TilePivot      { get { return tilePivot; } }
    public short    IndexFlag     { get { return tileIndexFlag; } }
    public short    InfoFlag      { get { return tileInfoFlag;  } }
    public long     CollisionFlag { get { return collisionFlag; } }
    public bool     IsHalfScale   { get { return isHalfScale;   } }
    public MeshFilter     MeshFilter { get { return meshFilter; } }


    // debug : 얘도 교통정리 해야겠구만.
    //[HideInInspector] public Vector3     debug_gridPivot;
    //[HideInInspector] public short       debug_gridIndex;
    //[HideInInspector] public MapTileData debug_data;
    [HideInInspector] public bool IsHalf => isHalfScale;

    //처음부터 bit_flag 사용하지 말고, 저장할 때에 했어도 좋았을 듯?

    public void Init()
    {
        /* mesh */
        meshFilter = transform.GetComponent<MeshFilter>();

        /* transform => tile pivot*/
        tilePivot = GetTilePivotFromCenter(transform.position);

        /* grid index flag */
        int grid_x = Mathf.FloorToInt(tilePivot.x / 32);
        int grid_y = Mathf.FloorToInt(tilePivot.y / 4);
        int grid_z = Mathf.FloorToInt(tilePivot.z / 32);
        gridIndexFlag = GetGridIndexFlag(grid_x, grid_y, grid_z);


        /* tile index flag */
        gridPivot = new Vector3(grid_x * 32, grid_y * 4, grid_z * 32).Truncate();
        Vector3Int diffInt = (tilePivot - gridPivot).ToInt();
        tileIndexFlag = GetTileIndexFlag(diffInt);

        /* tile info */
        tileInfoFlag = GetTileInfoFlag();

        /* collide */
        collisionFlag = GetCollideFlag(tilePivot);

        /* for debug */
        //debug_gridIndex = gridIndexFlag;
        //debug_gridPivot = gridPivot;
        //debug_data = new MapTileData(tileIndexFlag, tileInfoFlag, collisionFlag);



        // 필요한 정보를 꺼내어 주면 되겠구나..?
        // grid index
        // tile index
        // tile info
        // collision
        // mesh
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
        int shiftIsHalfScale = 15;
        int shiftTileX       =  9;
        int shiftTileY       =  6;
        int shiftTileZ       =  0;

        int tileFlag = 0;
        tileFlag |= (isHalfScale == true) ? 1 << shiftIsHalfScale : 0;
        tileFlag |= (diffInt.x) << shiftTileX;
        tileFlag |= (diffInt.y) << shiftTileY;
        tileFlag |= (diffInt.z) << shiftTileZ;

        return (short)tileFlag;
    }
    private short GetTileInfoFlag()
    {
        int shiftMeshLayer    = 13;
        //int shiftTriggerType  =  9;
        //int shiftTriggerValue =  0;

        int infoFlag = 0;
        infoFlag |= meshLayer << shiftMeshLayer;
        // 차라리 필드의 속성(ex. 비, 눈, 진흙, .. 을 넣는게 좋겠다.)
        //infoFlag |= (int)triggerType << shiftTriggerType;
        //infoFlag |= triggerValue << shiftTriggerValue;

        return (short)infoFlag;
    }
    private long GetCollideFlag(Vector3 tilePivot)
    {
        Mesh mesh = meshFilter.sharedMesh;
        long collide = 0;
        Quaternion rot     = transform.rotation;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals  = mesh.normals;
        int[] triangles    = mesh.triangles;

        for (int t = 0; t < triangles.Length; t += 3)
        {
            int t0 = triangles[t];
            int t1 = triangles[t + 1];
            int t2 = triangles[t + 2];

            //Determine whether the mesh is the target for sampling by normal value.
            Vector3 normal1 = rot * normals[t0];
            Vector3 normal2 = rot * normals[t1];
            Vector3 normal3 = rot * normals[t2];
            Vector3 normal = normal1;
            if (normal2.y < normal.y) { normal = normal2; }
            if (normal3.y < normal.y) { normal = normal3; }
            normal = normal.Truncate();

            if (0 >= normal.y)
            {
                continue;
            }

            float margin = 0.125f;
            Vector3 A = GetSnappingPoint(transform.TransformPoint(vertices[t0]), margin);
            Vector3 B = GetSnappingPoint(transform.TransformPoint(vertices[t1]), margin);
            Vector3 C = GetSnappingPoint(transform.TransformPoint(vertices[t2]), margin);

            float scale = isHalfScale ? 0.5f : 1f;
            collide |= GetTileDataRecursive(0, tilePivot, A, B, C, scale);
        }

        return collide;
    }
    private Vector3 GetSnappingPoint(Vector3 p, float margin)
    {
        float x = p.x;
        float y = p.y;
        float z = p.z;
        float diff;

        //Similar to rounding, but the standard is different for each dist, not 0.5f.
        diff = x % margin;
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
        float v0to1 = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p1.x, 0, p1.z)).Truncate();
        float v1to2 = Vector3.Distance(new Vector3(p1.x, 0, p1.z), new Vector3(p2.x, 0, p2.z)).Truncate();
        float v0to2 = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p2.x, 0, p2.z)).Truncate();

        float diagonal = v1to2;
        Vector3 swap;

        //빠른 탐색을 위하여 꼭지점의 각이 직각인 점을 v0로 설정한다. (모든 삼각형이 직각 이등변 삼각형이라 가능함.)
        if (diagonal < v0to1)
        {
            swap = p2;
            p2 = p0;
            p0 = swap;

            diagonal = v0to1;
        }
        if (diagonal < v0to2)
        {
            swap = p1;
            p1 = p0;
            p0 = swap;

            diagonal = v0to2;
        }

        float scale_half   = scale * 0.5f;
        float scale_quater = scale * 0.25f;

        //삼각형 중 가장 긴 변이 단위 길이(scale_half)보다 같거나 짧을 때까지 재귀호출
        if (scale_half < diagonal)
        {
            Vector3 midPoint = ((p1 + p2) * 0.5f).Truncate();
            collide |= GetTileDataRecursive(collide, pivot, p0, p1, midPoint, scale);
            collide |= GetTileDataRecursive(collide, pivot, p0, p2, midPoint, scale);
        }
        else
        {
            //get point, get pivot
            Vector3 pointCenter = GetSnappingPoint((p0 + p1 + p2) * 0.333f, scale_quater/* * 0.5f*/);

            //set flag
            long movable = 1 << TileUtility.GetTriangleIndex((pointCenter - pivot).Truncate(), scale_half);
            collide |= movable << (13 * 3);

            long height = 0;

            float scale_quater_inverse = (1 / scale_quater).Truncate();
            height |= GetHeightFlag(p0 - pivot, scale_quater_inverse);
            height |= GetHeightFlag(p1 - pivot, scale_quater_inverse);
            height |= GetHeightFlag(p2 - pivot, scale_quater_inverse);
            collide |= height;
        }

        return collide;
    }
    private long GetHeightFlag(Vector3 diff, float scale_quater_inverse)
    {
        diff = diff.Truncate();
        int  x = (int) (diff.x * scale_quater_inverse);
        long y = (long)(diff.y * scale_quater_inverse);  //y: 0 ~ 4 (0b000 ~ 0b100)
        int  z = (int) (diff.z * scale_quater_inverse);

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
        if (a.gridIndexFlag != b.gridIndexFlag)
        {
            return a;
        }

        if (a.tileIndexFlag != b.tileIndexFlag)
        {
            return a;
        }

        a.tileInfoFlag  |= b.tileInfoFlag;
        a.collisionFlag |= b.collisionFlag;
        return a;
    }
}
