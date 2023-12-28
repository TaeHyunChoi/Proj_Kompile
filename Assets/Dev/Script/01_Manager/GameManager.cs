using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager
{
    private Transform transform;
    private OnOpening opening;
    private ContentType current;
    private ISequenceUpdater sequence;

    public GameManager(Transform transform)
    {
        this.transform = transform;
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
        yield return new WaitUntil(() => openingTask.IsCompletedSuccessfully);

        opening = openingTask.Result;
        sequence = opening as ISequenceUpdater;
        opening.Start();
        yield return new WaitUntil(() => titleTask.IsCompletedSuccessfully);

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

    public void SetCurrent(ContentType type, out InputDele func)
    {
        current = type;
        func = null;

        switch (type)
        {
            case ContentType.Opening: func = opening.Input;  break;
        }
    }

    public void Update()
    {
        sequence.Update();
    }
}