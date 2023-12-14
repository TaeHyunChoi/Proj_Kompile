using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class OnOpening
{
    //오프닝은 3개 타입의 애니메이션을 가진다. (로고, 전투, 타이틀 화면)
    //로고+전투를 스킵할 수 있으니까 타이틀 화면은 ...
    //와 이거 왤케 어렵냐 흠
    //오프닝은 1개 UI 타입을 가진다 (UITitle)

    private GameObject[] prefab;

    public async Task<bool> InitAsync(Transform root)
    {
        try
        {
            //이렇게 하면 Scene이 완성되기 전에 호출이 되어서 오브젝트를 올릴 수 없다는 말 같은데?

            //Addressable.LoadAssetAsync()는 Unity API이므로 Task.Run()을 통해 다른 쓰레드에서 실행시킬 수 없다.
            //Task<GameObject> logo = Task.Run<GameObject>(() => AssetManager.InstantiateAsync("OpeningFade", root, isActive: true));

            //그래서 사실상 얘네들도 메인 쓰레드에서 처리하는 셈이다?
            Task<GameObject> logoTask = AssetManager.InstantiateAsync("OpeningFade", root, isActive: true);
            Task<GameObject> introPlayTask = AssetManager.InstantiateAsync("UIBattle", root);
            Task<GameObject> titleTask = AssetManager.InstantiateAsync("UnitBase", root);

            //완료하면 GC에서 알아서 메모리 해제한다고 한다.
            prefab = await Task.WhenAll(logoTask, introPlayTask, titleTask);
            Debug.Log("InitAsync() : " + Thread.CurrentThread.ManagedThreadId);
            foreach (var go in prefab)
            {
                if (go == null)
                {
                    return false;
                }
                Debug.Log(go.name);
            }

            //로드 성공 후 처리
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading assets: " + ex.Message);
            return false;
        }
    }
    public void Play()
    {
        Debug.Log("Play Opening");
        //순서대로 실행시키면 되는 것이지요...?
        //Animation에도 콜백을 붙일 수 있나?
        //안되면 고민 좀;
    }
}