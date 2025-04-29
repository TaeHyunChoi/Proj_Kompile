using Script.Manager;
using System.Threading.Tasks;
using UnityEngine;

public class IngameFieldPlayer : _IngameUnitBase
{
    private Animator animator;
    private (int code, RuntimeAnimatorController ctrl) anime_data;

    public async Task<bool> Init()
    {
        animator = transform.GetComponent<Animator>();
        anime_data = await AssetManager.LoadAsset<RuntimeAnimatorController>("AnimCtrl_Ataho");

        animator.runtimeAnimatorController = anime_data.ctrl;
        //SetAnime();

        return true;
    }

    public void SetAnime()
    {
        animator.Play("Anim_Ataho_Move_Front");
    }
}
