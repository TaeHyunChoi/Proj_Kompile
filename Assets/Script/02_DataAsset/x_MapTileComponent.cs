using System;
using UnityEngine;
using static Index.IDxTile;

[Obsolete]
public class x_MapTileComponent : MonoBehaviour
{
    /*
    [SerializeField]
    private byte layer;
    public byte Layer { get => layer; }

#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    [SerializeField] 
    private float scale = 1f;
    [SerializeField] 
    private ETileTriggerType trigger;
    [SerializeField] 
    private byte valueLayer;
    [SerializeField] 
    private int  valueInteract;
#endif
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
    private void Awake()
    {
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;

        int info = (1f != scale) ? (1 << SHIFT_INFO_SCALE) : 0;
        int trigger = (int)this.trigger;

        if (0 != (ETileTriggerType.Scale & this.trigger))
        {
            int scaleDown = (1f == scale) ? 1 : 0;
            trigger |= scaleDown << SHIFT_TRIGGER_SCALE_VALUE;
        }
        if (0 != (ETileTriggerType.Layer & this.trigger))
        {
            trigger |= valueLayer << SHIFT_TRIGGER_LAYER_VALUE;
        }
        if (0 != (ETileTriggerType.Event & this.trigger))
        {
            trigger |= valueInteract << SHIFT_TRIGGER_INTERACT_VALUE;
        }

        x_Dev_MapSampler.InitTile(transform, mesh, scale, layer, info, trigger);
    }
#endif
        */
}
public class TransMapTile : IRoutineUpdater
{
    private readonly float fadeSpeed = 5f;

    private GameObject gameObject;
    private Material   material;
    private Color      color;

    public TransMapTile(x_MapTileComponent tile)
    {
        gameObject = tile.gameObject;
        material = tile.GetComponent<MeshRenderer>().material;
        color = material.color;
        material.color = new Color(color.r, color.g, color.b, 0f);
        gameObject.SetActive(true);
    }

    public int MoveNext(int index)
    {
        switch (index)
        {
            case 0:
                float alpha = material.color.a;
                if (1f > alpha)
                {
                    alpha += Time.fixedDeltaTime * fadeSpeed;
                    material.color = new Color(color.r, color.g, color.b, alpha);
                    return index;
                }
                material.color = new Color(color.r, color.g, color.b, 1f);
                break;
            default:
                return -1;
        }

        return index + 1;
    }
}
