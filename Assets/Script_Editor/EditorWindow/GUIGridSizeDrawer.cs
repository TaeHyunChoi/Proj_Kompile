#if UNITY_EDITOR
namespace Kompile.Map.Editor.Tools
{
    using UnityEngine;
    using static Kompile.Data.MapConsts;

    public class GUIGridSizeDrawer : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Vector3 size = GRID_SIZE * Vector3.one;
            Vector3 center = size * 0.5f;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + center, size);
        }
    }
}
#endif