using Script.Manager;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Script.Interface;
using Script.Index;
using Script.IngameMessage;
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

        SetAnime("Anim_Ataho_Idle_Front"); // 이게 안 먹은건가?
        return true;
    }

    // 나중에 직접 조작 캐릭터(Playable)과 동료(Follower)를 분리해서 구현해야겠군..
    public bool ReceiveInput(InputFlag inputFlag)
    {
        Vector3 dir = Vector3.zero;
        if (true == inputFlag.Contains(InputFlag.UP))    { dir += Vector3.forward; }
        if (true == inputFlag.Contains(InputFlag.DOWN))  { dir += Vector3.back;    }
        if (true == inputFlag.Contains(InputFlag.LEFT))  { dir += Vector3.left;    }
        if (true == inputFlag.Contains(InputFlag.RIGHT)) { dir += Vector3.right;   }

        dir.Normalize();
        transform.position += 10f * Time.deltaTime * dir;

        // 여기서 애니메이션도 4방향 또는 8방향으로 설정하면 된다..
        // 8방향 애니메이션 만들 수 있으면 참 좋을텐데~ AI로 구현 가능?

        return true;
    }

    public void SetAnime(string key)
    {
        animator.Play(key);
    }
}
