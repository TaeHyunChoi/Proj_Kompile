using Script.Manager;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Script.Interface;
using Script.Index;
using static Script.Index.IDxInput;

public class IngameFieldPlayer : IngameUnitBase, IInputReceiver
{
    private Animator animator;
    private int index;

    public async Task<bool> Init(int index)
    {
        this.index = index;
    
        asset_hash_codes = new List<int>();

        animator = transform.GetComponent<Animator>();
        var (hashCode, value) = await AssetManager.LoadAssetAsync<RuntimeAnimatorController>("AnimCtrl_Ataho");
        asset_hash_codes.Add(hashCode);
        animator.runtimeAnimatorController = value;

        SetAnime("Anim_Ataho_Idle_Front");
        return true;
    }

    public bool ReceiveInput(InputFlag inputFlag)
    {
        switch (index)
        {
            case 0:
                Vector3 dir = Vector3.zero;
                if (true == inputFlag.Contains(InputFlag.UP))    { dir += Vector3.forward; }
                if (true == inputFlag.Contains(InputFlag.DOWN))  { dir += Vector3.back; }
                if (true == inputFlag.Contains(InputFlag.LEFT))  { dir += Vector3.left; }
                if (true == inputFlag.Contains(InputFlag.RIGHT)) { dir += Vector3.right; }
                dir.Normalize();

                Vector3 nextPosition = transform.position + base.moveSpeed * Time.deltaTime * dir;
                if (true == FieldManager.TryMovePlayer(nextPosition, out float y))
                {
                    transform.position = new Vector3(nextPosition.x, y, nextPosition.z);
                    return true;
                }
                break;
        }

        return false;
    }
    public void SetAnime(string key)
    {
        animator.Play(key);
    }
}
