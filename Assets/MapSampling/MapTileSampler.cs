using CMathf;
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

    private short gridIndex;
    private short tileIndex;
    private long  collide;

    public void Set()
    {
        var scale = isHalfScale ? 0.5f : 1f;

        Vector3 center = CMath.Truncate(transform.position, exponent: 3);
        var x = center.x;
        var y = center.y;
        var z = center.z;
        var sign_x = x < 0 ? -1 : 1;
        var sign_y = y < 0 ? -1 : 1;
        var sign_z = z < 0 ? -1 : 1;

        /* tile pivot */
        var tile_x     = x - sign_x * (x % scale);
        var tile_y     = y - sign_y * (y % scale);
        var tile_z     = z - sign_z * (z % scale);
        var tile_pivot = new Vector3(tile_x, tile_y, tile_z);
        if (true == isHalfScale)
        {
            tile_pivot += new Vector3(0.125f, 0, 0);
        }

        /* grid pivot */
        var grid_x = Mathf.Floor(x / 32);
        var grid_y = Mathf.Floor(y / 4);
        var grid_z = Mathf.Floor(z / 32);
        var grid_pivot = new Vector3(grid_x, grid_y, grid_z);
        grid_pivot = CMath.Truncate(grid_pivot);
        
        Debug.Log($"{transform.position:F3} * {scale}f\ngrid_pivot:{grid_pivot:F3}, tile_pivot:{tile_pivot:F3}");

        /* pivot diff */
        var diff = tile_pivot - grid_pivot;

        /* grid index, tile index*/


        // info = layer, trigger_type, trigger_value

    }
}
