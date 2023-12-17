using UnityEngine;
using System.Threading.Tasks;
using System;

internal class OnTitle : ContentBase
{
    //코드 중복?
    public static async Task<bool> InitAsync(Transform canvas_ui)
    {
        try
        {
            GameObject obj = await AssetManager.InstantiateAsync("UITitle", canvas_ui);
            obj.AddComponent<OnTitle>();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading assets: " + ex.Message);
            return false;
        }

        return true;
    }
    public override void Start()
    {

    }

    public override void Update()
    {

    }
    public override void InputEvent(int input)
    {

    }
    public override void End()
    {
        //타이틀은 인게임 내에서 사용이 적으니 개체 비활성화가 아니라 해제가 좋겠다.
    }
}
