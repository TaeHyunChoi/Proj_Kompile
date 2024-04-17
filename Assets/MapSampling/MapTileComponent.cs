using UnityEngine;
using static Index.IDxTile;

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
    private byte valueLayer;
    [SerializeField] 
    private int  valueInteract;
    private Mesh mesh;

    private void Awake()
    {
        mesh = transform.GetComponent<MeshFilter>().mesh;

        int info = (1f != scale) ? (1 << SHIFT_INFO_SCALE) : 0;
        int trigger = (int)this.trigger;

        if (0 != (TileTrigger.Scale & this.trigger))
        {
            int scaleDown = (1f == scale) ? 1 : 0;
            trigger |= scaleDown << SHIFT_TRIGGER_SCALE_VALUE;
        }
        if (0 != (TileTrigger.Layer & this.trigger))
        {
            trigger |= valueLayer << SHIFT_TRIGGER_LAYER_VALUE;
        }
        if (0 != (TileTrigger.Interact & this.trigger))
        {
            trigger |= valueInteract << SHIFT_TRIGGER_INTERACT_VALUE;
        }

        Dev_MapSampler.InitTile(transform, mesh, scale, layer, info, trigger);
#endif
    }
}
