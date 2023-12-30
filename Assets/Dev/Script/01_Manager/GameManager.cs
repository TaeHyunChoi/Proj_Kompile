using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager
{
    private ISequenceUpdater[] sequence;
    private int sequenceCount;

    public GameManager(Transform transform)
    {
        sequence = new ISequenceUpdater[2];
        sequenceCount = 0;
    }

public void Set(ContentType type)
{
        IEnumerator coroutine;
        switch (type)
        {
            case ContentType.Opening: coroutine = IEInitOpeningAsync(); break;
            default: /* Do Nothing. */ return;
        }

        Coroutiner.PlayCoroutine(coroutine);    
}
    private IEnumerator IEInitOpeningAsync()
    {
        UIManager uiMgr = Main.UIMgr;
        Transform parentIsCameraCanvas = uiMgr.GetCameraCanvas().transform;        
        Task<OnOpening> openingTask = OnOpening.InitAsync(parentIsCameraCanvas);
        yield return new WaitUntil(() => openingTask.IsCompletedSuccessfully);

        OnOpening opening = openingTask.Result;
        sequence[sequenceCount++] = opening as ISequenceUpdater;
        opening.Start();

        openingTask.Dispose();        
    }
    public void Update()
    {
        for (int i = 0; i < sequenceCount; ++i)
        {
            sequence[i].Update();
        }
    }

    public InputDele GetInputDele(ContentType type)
    {
        switch(type)
        {
            case ContentType.Opening: return OnOpening.Input; 
        }

        return null;
    }
}