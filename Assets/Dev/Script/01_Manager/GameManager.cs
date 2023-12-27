using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public class GameManager : MonoBehaviour
{
    private ContentType currentContent;

    public void InitContent(ContentType type)
    {
        IEnumerator coroutine;
        switch (type)
        {
            case ContentType.Opening:   coroutine = IEInitOpeningAsync();     break;
            case ContentType.Field:     coroutine = IEInitFieldAsync();     break;
            default:                    /* Do Nothing. */               return;
        }

        currentContent = type;
        StartCoroutine(coroutine);
    }
    private IEnumerator IEInitOpeningAsync()
    {
        UIManager uiMgr = Main.UIMgr;
        Transform transformCameraCanvas = uiMgr.GetCameraCanvas().transform;

        Task<OnOpening> taskOpening = OnOpening.InitAsync(transformCameraCanvas);
        Task<UITitle>   taskUITitle = uiMgr.InitAsync<UITitle>(UIType.Title, transformCameraCanvas, false);
        yield return new WaitUntil(() => taskOpening.IsCompleted && taskUITitle.IsCompleted);

        taskOpening.Result.Play();

        taskOpening.Dispose();
        taskUITitle.Dispose();
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
}