using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager
{
    private ISequenceUpdater sequence;
    public void SetSequence(ISequenceUpdater content)
    {
        sequence = content;
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
        switch (type)
        {
            case ContentType.Opening: return OnOpening.Input;
            case ContentType.Field:   return InField.Input;
        }

        return null;
    }
    public void Dispose()
    {
        if(sequence != null)
        {
            sequence.Close();
        }
    }
}