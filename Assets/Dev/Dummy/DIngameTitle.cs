public class DIngameTitle : DIngameContent
{
    public override void Start()
    {
        // Opening: Company Logo + GamePlay
        // Title Logo + UIManager.Open(Title);
    }
    public override void Update(int input)
    {
        //입력 처리
        if (input != 0)
        { 
            //개별 조작에 대하여 처리
        }

        //콘텐트 처리

    }
    public override void End()
    {
        //타이틀은 인게임 내에서 사용이 적으니 개체 비활성화가 아니라 해제가 좋겠다.
    }
}
