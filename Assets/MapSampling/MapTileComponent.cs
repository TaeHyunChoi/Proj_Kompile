using UnityEngine;


public class MapTileComponent : MonoBehaviour
{
    [SerializeField]
    private byte layer;
    public byte Layer { get => layer; }

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    [SerializeField]
    private float scale = 1f;
    [SerializeField]
    private TileTrigger trigger;
    [SerializeField]
    private byte triggerValue;
    private void Awake()
    {
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
        //transform.position = PTile.SnappingPoint(transform.position, scale, 2);
        Dev_MapSampler.InitTile(transform, mesh, scale, layer, (byte)trigger, triggerValue);
#endif
    }
}
