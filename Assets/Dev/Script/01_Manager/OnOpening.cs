using System;
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
            Task<GameObject> logoTask = AssetManager.InstantiateAsync("OpeningFade", root, isActive: true);
            Task<GameObject> introPlayTask = AssetManager.InstantiateAsync("OpeningFade", root);
            Task<GameObject> titleTask = AssetManager.InstantiateAsync("OpeningFade", root);

            //완료하면 GC에서 알아서 메모리 해제한다고 한다.
            prefab = await Task.WhenAll(logoTask, introPlayTask, titleTask); 
            foreach (var go in prefab)
            {
                if (go == null)
                {
                    return false;
                }
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
}