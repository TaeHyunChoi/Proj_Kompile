using Script.Manager;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Script.Interface;
using Script.Index;
using static Script.Index.IDxInput;

public class IngameFieldPlayer : IngameUnitBase, IInputReceiver, IIngameFixedUpdater
{
    private Animator animator;
    private int index;

    private Vector3 direction;

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

                direction = dir;
                return true;
            default:
                break;
        }

        return false;
    }
    public IngameUpdateState FixedUpdateState()
    {
        //Vector3 nextPosition = transform.position + base.moveSpeed * Time.fixedDeltaTime * direction;
        //if (true == FieldManager.TryMovePlayer(nextPosition, out float y))
        if(true == FieldManager.TryMovePlayer(transform.position,direction, moveSpeed * Time.fixedDeltaTime, out float y))
        {
            Vector3 nextPosition = transform.position + base.moveSpeed * Time.fixedDeltaTime * direction;
            transform.position = new Vector3(nextPosition.x, y, nextPosition.z);
        }

        direction = default;
        return IngameUpdateState.RUNNING;
    }

    public void SetAnime(string key)
    {
        animator.Play(key);
    }
}
