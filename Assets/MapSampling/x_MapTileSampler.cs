using CMathf;
using UnityEngine;

public class x_MapTileSampler : MonoBehaviour
{
    [SerializeField]
    private bool mIsHalfScale;
    [SerializeField]
    private byte mMeshLayer;
    [SerializeField]
    private x_ETileTriggerType mTriggerType; // : byte
    [SerializeField]
    private byte mTriggerIndex;

    private short mGridIndex;
    private short mTileIndex;
    private long  mCollider;

    public void Set()
    {
        // 음수는 좀 다를 것 같으니 양수 기준으로 테스트
        Vector3 center = CMath.FloorToVector(transform.position, 3);
        var pivot_x = CMath.FloorToInt(center.x, mIsHalfScale ? 2 : 0);
        var pivot_y = CMath.FloorToInt(center.y, mIsHalfScale ? 2 : 0);
        var pivot_z = CMath.FloorToInt(center.z, mIsHalfScale ? 2 : 0);
        var tile_pivot = new Vector3(pivot_x, pivot_y, pivot_z);

        // grid index (sign_5bits, sign_3bits, sign_5bits)
        var grid_x = CMath.FloorToInt(tile_pivot.x / 32, mIsHalfScale ? 2 : 0);
        var grid_y = CMath.FloorToInt(tile_pivot.y /  8, mIsHalfScale ? 2 : 0);
        var grid_z = CMath.FloorToInt(tile_pivot.z / 32, mIsHalfScale ? 2 : 0);
        var grid_index = new Vector3(grid_x, grid_y, grid_z);

        // tile index
        var diff = tile_pivot - new Vector3(grid_x * 32, grid_y * 8, grid_z * 32);
        var tile_x = diff.x;
        var tile_y = diff.y;
        var tile_z = diff.z;

        Debug.Log($"grid[{grid_x}, {grid_y}, {grid_z}] => tile[{tile_x}, {tile_y}, {tile_z}]");

        // info = layer, trigger_type, trigger_value

    }
}
