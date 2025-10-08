using Script.Manager;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Script.Interface;
using Script.Index;
using static Script.Index.IDxInput;
using Unity.Mathematics;

public class IngameFieldPlayer : IngameUnitBase, IInputReceiver, IIngameFixedUpdater
{
    private Animator animator;
    private int index;

    private float3 direction;

    public async Task<bool> Init(int index)
    {
        this.index = index;
        asset_hash_codes = new List<int>();

        animator = transform.GetComponent<Animator>();
        var (hashCode, value) = await AssetManager.LoadAssetAsync<RuntimeAnimatorController>("AnimCtrl_Ataho");
        asset_hash_codes.Add(hashCode);
        animator.runtimeAnimatorController = value;

#if UNITY_EDITOR
        transform.position = new Vector3(1f, 0f, 5f);
        UnityEngine.Debug.Log($"Set position for test play; {transform.position}");
#endif

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
        if (true == direction.Equals(default))
        {
            return IngameUpdateState.RUNNING;
        }

        float3 position = transform.position;
        float3 target_position = position  + (moveSpeed * Time.fixedDeltaTime) * direction;

        UnityEngine.Debug.Log($"[MoveOnTile] before: {transform.position}");
        if (true == FieldManager.TryPlayerMove(target_position, out float y))
        {
            transform.position = new Vector3(target_position.x, y, target_position.z);
        }

        direction = new float3(0, 0, 0);
        return IngameUpdateState.RUNNING;
    }

    public void SetAnime(string key)
    {
        animator.Play(key);
    }
}
