using UnityEngine;


public class MapTileComponent : MonoBehaviour
{
    [SerializeField]
    private byte layer;
    public byte Layer { get => layer; }

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    [SerializeField]
    private TileTrigger trigger;
    [SerializeField]
    private byte triggerValue;
    private void Awake()
    {
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
        float scale = (0 != (byte)(TileTrigger.Small & trigger)) ? 0.5f : 1f;
        //transform.position = PTile.SnappingPoint(transform.position, scale, 2);
        Dev_MapSampler.InitTile(transform, mesh, scale, layer, (byte)trigger, triggerValue);
#endif
    }
}
