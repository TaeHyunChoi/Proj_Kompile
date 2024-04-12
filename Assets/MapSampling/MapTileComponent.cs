using UnityEngine;
using static Index.IDxTile;

public class MapTileComponent : MonoBehaviour
{
    [SerializeField]
    private byte layer;
    public byte Layer { get => layer; }

    private Mesh mesh;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    [SerializeField] private float scale = 1f;

    [SerializeField] private TileTrigger trigger;
    [SerializeField] private bool booleanScaleDown;
    [SerializeField] private byte valueLayer;
    [SerializeField] private int  valueInteract;
#endif

    private void Awake()
    {
        mesh = transform.GetComponent<MeshFilter>().mesh;

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        int info = (int)trigger;

        //info.scale
        if (1f != scale)
        {
            info |= 1 << SHIFT_INFO_SCALE;
        }

        //info.trigger
        if (TileTrigger.ScaleDown != trigger)
        {
            byte scaleDown = (true == booleanScaleDown) ? (byte)1 : (byte)0;
            info |= scaleDown << SHIFT_TRIGGER_SCALE_VALUE;
        }

        if (TileTrigger.Layer != trigger)
        {
            info |= valueLayer << SHIFT_TRIGGER_LAYER_VALUE;
        }

        if (TileTrigger.Interact != trigger)
        {
            info |= valueInteract << SHIFT_TRIGGER_INTERACT_VALUE;
        }

        Dev_MapSampler.InitTile(transform, mesh, scale, layer, info);
#endif
    }
}
