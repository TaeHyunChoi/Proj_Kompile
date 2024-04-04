using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager
{
    private ISequenceUpdater sequence;
    private Transform transform;

    private MapData map;
    private Unit player;

    public GameManager(Transform transform)
    {
        this.transform = transform;
    }
    public void SetSequence(ISequenceUpdater content)
    {
        sequence = content;
    }
    public void SetMap(MapData mapData)
    {
        map = mapData;
    }
    public Transform GetTransform()
    {
        return transform;
    }

    public void Start()
    {
        sequence.Start();
    }
    public void Update()
    {
        sequence.Update();
    }
    public InputDele GetInputDele(ContentType type)
    {
        InputDele dele = null;
        switch (type)
        {
            case ContentType.Opening:   return OnOpening.Input;
            case ContentType.Field:     dele = InField  .Input;     break;
            case ContentType.Battle:    dele = InBattle .Input;     break;
        }

        //dele += SystemOption.Input;
        return dele;
    }
    public void Dispose()
    {
        if(sequence != null)
        {
            sequence.Close();
        }
    }
}
