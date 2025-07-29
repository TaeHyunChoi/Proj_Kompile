using Script.Manager;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class IngameFieldPlayer : IngameUnitBase
{
    private Animator animator;

    public async Task<bool> Init()
    {
        asset_hash_codes = new List<int>();

        animator = transform.GetComponent<Animator>();
        var (hashCode, value) = await AssetManager.LoadAssetAsync<RuntimeAnimatorController>("AnimCtrl_Ataho");
        asset_hash_codes.Add(hashCode);
        animator.runtimeAnimatorController = value;

        SetAnime("Anim_Ataho_Move_Front");
        return true;
    }

    public void SetAnime(string key)
    {
        animator.Play(key);
    }
}
