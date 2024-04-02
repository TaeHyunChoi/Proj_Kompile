using UnityEngine;


public class MapTileComponent : MonoBehaviour
{
    [SerializeField]
    private TileFeature status;
    [SerializeField]
    private byte layer;

    private Mesh  mesh;
    private int key; //Field에서 mesh를 on/off할 때에 사용

    private void Awake()
    {
        mesh = transform.GetComponent<MeshFilter>().mesh;

        float scale = (0 != (byte)(TileFeature.Small & status)) ? 0.5f : 1f;
        Vector3 position = PTile.SnappingPoint(transform.position, scale, 2);
        key = (layer << 24) | PTile.GetKey(position, scale);

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        Dev_MapSampler9th.InitTile(transform, mesh, scale, layer, (byte)status);
#endif
    }
}
