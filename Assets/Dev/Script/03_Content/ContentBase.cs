/// <summary>
/// Main Game Loop를 돌리는 콘텐츠.
/// 각 콘텐츠별로 개체를 나누어 구현/실행한다.
/// ex. 필드는 InField:ContentBase, 전투는 InBattle:ContentBase, ...
/// </summary>
public abstract class ContentBase
{
    public abstract void Start();
    public abstract void Update();
    public abstract void InputEvent(int input);
    public abstract void End();
}