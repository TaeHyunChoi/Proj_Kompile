
using static Index.IDxInput;

public class MapSceneManager_
{
    public void PlayOpening()
    {
        //프리팹 생성부터 쭉쭉- 이걸 어떻게 묶어야 가독성이 좋아지려나..
        //이거 코루틴 3개 묶어서 처리하는 것도 가능하긴 한디..
        //OpeningLogo logo = new OpeningLogo(transform.GetChild(0));
        //CoroutineUpdater.SetHandler(new CCoroutine<OpeningLogo>(logo));
    }

    public void InputOpening(EInput input)
    {
        //Opening coroutine에다가 input값을 넘겨서
        //index를 씹어야 하는 그런거구만..? => 어차피 input 들어오는지 확인하겠군.
    }
}
