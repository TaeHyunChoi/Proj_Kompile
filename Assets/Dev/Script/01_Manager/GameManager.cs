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
            case ContentType.Opening:   coroutine = IETitleAsync();     break;
            case ContentType.Field:     coroutine = IEFieldAsync();     break;
            default:                    /* Do Nothing. */               return;
        }

        currentContent = type;
        StartCoroutine(coroutine);
    }
    private IEnumerator IETitleAsync()
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
    private IEnumerator IEFieldAsync()
    {
        //Enter와 Runtime에서 호출하는 FieldFunc가 다르다. 생각해야 한다~
        //페이드 암전
        //씬 변경
        //UITitle.Close();

        yield break;
    }
}