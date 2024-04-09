using UnityEngine;


public class MapTileComponent : MonoBehaviour
{
    [SerializeField]
    private byte layer;
    public byte Layer { get => layer; }

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    [SerializeField]
    private TileFeature status;
#endif

    //private Mesh mesh;
    //private int key;
    //public int Key { get => key; }

    private void Awake()
    {
        Set(0 == layer);
        //key = (layer << 24) | PTile.GetKey(position, scale);

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
        float scale = (0 != (byte)(TileFeature.Small & status)) ? 0.5f : 1f;
        Vector3 position = PTile.SnappingPoint(transform.position, scale, 2);
        Dev_MapSampler.InitTile(transform, mesh, scale, layer, (byte)status);
#endif
    }
    public void Set(bool isOn)
    {
        gameObject.SetActive(isOn);
    }
}
