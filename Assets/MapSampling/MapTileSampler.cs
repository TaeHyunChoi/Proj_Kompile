using CMathf;
using Unity.VisualScripting;
using UnityEngine;

public class MapTileSampler : MonoBehaviour
{
    [SerializeField]
    private ETileTriggerFlag triggerType; // : byte
    [SerializeField]
    private bool isHalfScale;
    [SerializeField]
    private byte meshLayer;
    [SerializeField]
    private byte triggerValue;

    private short gridFlagIndex;
    private short tileFlagIndex;
    private long  collide;

    public void Set()
    {
        var scale = isHalfScale ? 0.5f : 1f;

        Vector3 center = transform.position.Truncate();
        float sign_x = (center.x < 0) ? -1 : 1;
        float sign_y = (center.y < 0) ? -1 : 1;
        float sign_z = (center.z < 0) ? -1 : 1;

        /* tile pivot point */
        float tile_x = center.x - (sign_x * (center.x % scale));
        float tile_y = center.y - (sign_y * (center.y % scale));
        float tile_z = center.z - (sign_z * (center.z % scale));
        if (true == isHalfScale)
        {
            tile_x -= 0.25f;
        }

        /* grid flag */
        int grid_x = Mathf.FloorToInt(tile_x / 32);
        int grid_y = Mathf.FloorToInt(tile_y / 4);
        int grid_z = Mathf.FloorToInt(tile_z / 32);
        gridFlagIndex = SetGridFlag(grid_x, grid_y, grid_z);

        /* tile flag */
        Vector3 tile_pivot = new Vector3(tile_x, tile_y, tile_z).Truncate();
        Vector3 grid_pivot = new Vector3(grid_x * 32, grid_y * 4, grid_z * 32).Truncate();
        tileFlagIndex = SetTileIndex(tile_pivot - grid_pivot);

        // info = layer, trigger_type, trigger_value

    }

    private short SetGridFlag(int grid_x, int grid_y, int grid_z)
    {
        int BIT_GRID_X_SIGN = 15;
        int BIT_GRID_X = 10;
        int BIT_GRID_Y_SIGN = 9;
        int BIT_GRID_Y = 6;
        int BIT_GRID_Z_SIGN = 5;
        int BIT_GRID_Z = 0;

        int gridFlag = 0;

        if (grid_x < 0)
        {
            gridFlag |= 1 << BIT_GRID_X_SIGN;
            gridFlag |= (-grid_x) << BIT_GRID_X;
        }
        else
        {
            //gridFlag |= 0;
            gridFlag |= grid_x << BIT_GRID_X;
        }

        if (grid_y < 0)
        {
            gridFlag |= 1 << BIT_GRID_Y_SIGN;
            gridFlag |= (-grid_y) << BIT_GRID_Y;
        }
        else
        {
            //gridFlag |= 0;
            gridFlag |= grid_y << BIT_GRID_Y;
        }

        if (grid_z < 0)
        {
            gridFlag |= 1 << BIT_GRID_Z_SIGN;
            gridFlag |= (-grid_z) << BIT_GRID_Z;
        }
        else
        {
            //gridFlag |= 0;
            gridFlag |= grid_z << BIT_GRID_Z;
        }

        return (short)gridFlag;
    }
    private short SetTileIndex(Vector3 diff)
    {
        //플래그 뭐더라?
        return 0;
    }
}
