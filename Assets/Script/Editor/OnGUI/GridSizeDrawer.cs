#if UNITY_EDITOR

using UnityEngine;
using static Script.Index.Index;

public class GridSizeDrawer : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Vector3 size = new Vector3(GRID_X_LENGTH, GRID_Y_LENGTH, GRID_Z_LENGTH);
        Vector3 center = size * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + center, size);
    }
}

#endif