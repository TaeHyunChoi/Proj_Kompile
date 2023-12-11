using UnityEngine;

public class OnOpening : MonoBehaviour
{
    //오프닝은 3개 타입의 애니메이션을 가진다. (로고, 전투, 타이틀 화면)
    //로고+전투를 스킵할 수 있으니까 타이틀 화면은 ...
    //와 이거 왤케 어렵냐 흠
    //오프닝은 1개 UI 타입을 가진다 (UITitle)

    private int[] id;

    public OnOpening()
    {
        id = new int[3];
    }
    public async void InitAsync(Transform root)
    {
        //생성->재생: 로고
        //생성->재생: 게임 플레이
        //생성->재생: 타이틀 화면

        //흠... 흐음...
        //여기서 await를 걸면 안된다 >> 동시에 3개 불러서 그 다음에 Task.WhenAll(tasks); 걸어야 하지 않나
        GameObject go = await AssetManager.InstantiateAsync("OpeningFade", root);
        id[0] = go.GetInstanceID();
    }
}