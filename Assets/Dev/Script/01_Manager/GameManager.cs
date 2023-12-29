using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager
{
    private Transform transform;
    private OnOpening opening;
    private ContentType current;
    private ISequenceUpdater[] sequence;
    private int pointer;

    public GameManager(Transform transform)
    {
        this.transform = transform;
        sequence = new ISequenceUpdater[2];
        pointer = 0;
    }

    public void InitContent(ContentType type)
    {
        IEnumerator coroutine;
        switch (type)
        {
            case ContentType.Opening: coroutine = IEInitOpeningAsync(); break;
            case ContentType.Field:   coroutine = IEInitFieldAsync();   break;
            default: /* Do Nothing. */ return;
        }

        Coroutiner.PlayCoroutine(coroutine);
    }
    private IEnumerator IEInitOpeningAsync()
    {
        UIManager uiMgr = Main.UIMgr;
        Transform parentIsCameraCanvas = uiMgr.GetCameraCanvas().transform;

        Task<OnOpening> openingTask = OnOpening.InitAsync(parentIsCameraCanvas);
        Task<UITitle> titleTask = uiMgr.InitAsync<UITitle>(UIType.Title, parentIsCameraCanvas, false);
        Task<UISaveData> savedTask = uiMgr.InitAsync<UISaveData>(UIType.SaveData, parentIsCameraCanvas, false);
        yield return new WaitUntil(() => openingTask.IsCompletedSuccessfully);

        opening = openingTask.Result;
        sequence[pointer++] = opening as ISequenceUpdater;
        opening.Start();
        sequence[idxSequence++] = opening as ISequenceUpdater;
        yield return new WaitUntil(() => titleTask.IsCompletedSuccessfully && savedTask.IsCompletedSuccessfully);

        openingTask.Dispose();
        titleTask.Dispose();
    }
    private IEnumerator IEInitFieldAsync()
    {
        //입력 막고...
        Main.InputMgr.BlockInput();

        //페이드 암전 : 프리팹이든 뭐든 하나 만들어야겠구먼?
        //로딩 개체 하나 만들고.

        //Instantiate + Initialize
        //Content : 필드 생성, 초기화
        //UI : Main HUD, Option, Info, ...
        //yield return Wait All;

        //로딩창 해제 콜하고

        //입장 중에는 Blocked... 실행 중에는 InField.Input;

        yield break;
    }


    public void Update()
    {
        for (int i = 0; i < pointer; ++i)
        {
            sequence[i].Update();
        }
    }
    public void NewGame()
    {
        //로딩창 생성: 이거 기다려야 하는데? 코루틴..? 흠...
        //코루틴을 사용하지 않고 처리할 방법이 있는지
        //GameMgr.Update()로 로딩이 빠지는 셈인데...
        //로딩창도 ISequenceUpdate를 가질 수 있다. ㅇㅋ 납득.
        // => 지금 와서 보니 오히려 ContentOpening이 ISequenceUpdate보다 기능을 더 잘 표현한 듯?
        //Hmmmmmmm... Opening은 ISequenceUpdate를 가지면 안된다.
        //재설계 요망.

        //이전 콘텐츠 중지
        pointer -= 1;
        sequence[pointer].Stop();

        //필드 생성*초기화
        InitContent(ContentType.Field);
    }
}