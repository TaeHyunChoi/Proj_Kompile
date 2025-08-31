using Script.Manager;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Script.Interface;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Util;
using Script.Data;

public class IngameFieldPlayer : IngameUnitBase, IInputReceiver, IIngameFixedUpdater
{
    private Animator animator;
    private int index;

    private Vector3 last_input_position;

    public async Task<bool> Init(int index)
    {
        this.index = index;
        last_input_position = transform.position;

        asset_hash_codes = new List<int>();

        animator = transform.GetComponent<Animator>();
        var (hashCode, value) = await AssetManager.LoadAssetAsync<RuntimeAnimatorController>("AnimCtrl_Ataho");
        asset_hash_codes.Add(hashCode);
        animator.runtimeAnimatorController = value;

        SetAnime("Anim_Ataho_Idle_Front");
        return true;
    }

    // 나중에 직접 조작 캐릭터(Playable)과 동료(Follower)를 분리해서 구현해야겠군..
    // 아무런 입력이 없으면 호출되지 않는다.
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
                if (last_input_position != nextPosition)
                {
                    FieldManager.CheckPlayerMove(nextPosition);
                    last_input_position = nextPosition;
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

    public IngameUpdateState FixedUpdateState()
    {
        if (true == MapTileOverlapJobManager.Instance.CheckIfJobIsDone())
        {
            transform.position = last_input_position;
        }

        return IngameUpdateState.RUNNING;
    }
}
