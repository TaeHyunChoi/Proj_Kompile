
using UnityEngine;
using static Index.IDxInput;
using IECoroutine;

public class MapSceneManager_
{
    private CanvasGroup mLoadingCurtain;
    public MapSceneManager_(Transform mainTransform)
    {
        Transform canvasTransform = mainTransform.Find("CanvasCurtain");
        mLoadingCurtain = canvasTransform.GetComponentInChildren<CanvasGroup>();
    }

    public void LoadScene_Opening()
    {
        var opening = new IELoadOpeningScene(mLoadingCurtain);
        CoroutineUpdater.SetHandler(new CCoroutine<IELoadOpeningScene>(opening));
    }

    public void InputOpening(EInput input)
    {
        //Opening coroutine에다가 input값을 넘겨서
        //index를 씹어야 하는 그런거구만..? => 어차피 input 들어오는지 확인하겠군.
    }
}
