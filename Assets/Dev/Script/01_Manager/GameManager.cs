using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager
{
    private Transform transform;
    private OnOpening opening;
    private ISequenceUpdater[] sequence;
    private readonly int maxAcitvated = 2;
    private int idxSequence;

    public GameManager(Transform transform)
    {
        this.transform = transform;
        sequence = new ISequenceUpdater[maxAcitvated];
        idxSequence = 0;
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

        //OnOpening(currentContent)의 콘텐츠, 오브젝트 모두 파괴 및 해제
        //Init~ 단계이므로 OnOpening 없애는 게 강제된다.

        //페이드 암전 : 프리팹이든 뭐든 하나 만들어야겠구먼?

        //Instantiate + Initialize
        //Content : 필드 생성, 초기화
        //UI : Main HUD, Option, Info, ...
        //yield return Wait All;

        //로딩창 해제 콜하고

        //입장 중에는 Blocked... 실행 중에는 InField.Input;

        //씬 변경 : 
        //UITitle.Close();

        yield break;
    }


    public void Update()
    {
        for (int i = 0; i < idxSequence; ++i)
        {
            sequence[i].Update();
        }
    }
    public void StopSequence()
    {
        for (int i = 0; i < idxSequence; ++i)
        {
            sequence[i] = null;
        }
        idxSequence = 0;
    }
}