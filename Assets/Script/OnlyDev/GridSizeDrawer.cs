#if UNITY_EDITOR
using UnityEngine;

public class GridSizeDrawer : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Vector3 size = new Vector3(64f, 16f, 64f);
        Vector3 center = size / 2f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}

#endif