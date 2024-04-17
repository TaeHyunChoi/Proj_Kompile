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


    private void Awake()
    {
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;

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

public class TransMapTile : IUpdateRoutine
{
    private GameObject gameObject;
    private Material mat;
    private Color color;
    private float fadeSpeed = 5f;

    public TransMapTile(MapTileComponent tile)
    {
        gameObject = tile.gameObject;
        mat = tile.GetComponent<MeshRenderer>().material;
        color = mat.color;
        mat.color = new Color(color.r, color.g, color.b, 0f);
        gameObject.SetActive(true);
    }

    public int MoveNext(int index)
    {
        switch (index)
        {
            case 0:
                float alpha = mat.color.a;
                if (1f > alpha)
                {
                    alpha += Time.fixedDeltaTime * fadeSpeed;
                    mat.color = new Color(color.r, color.g, color.b, alpha);
                    return index;
                }
                alpha = 1f;
                break;
            default:
                return -1;
        }

        return index + 1;
    }
}
