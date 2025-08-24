using Script.Manager;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Script.Interface;
using Script.Index;
using static Script.Index.IDxInput;
using Script.Util;
using Script.Data;

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
        Move(transform.position + 10f * Time.deltaTime * dir);
        // 여기서 애니메이션도 4방향 또는 8방향으로 설정하면 된다..
        // 8방향 애니메이션 만들 수 있으면 참 좋을텐데~ AI로 구현 가능?

        return true;
    }

    private void Move(Vector3 next_position)
    {
        // 여기서부터 MapUtil 또는 FieldManager에 넘겨서 처리하면 좋을 듯

        // grid
        int scene_index = FieldManager.SceneIndex;
        int grid_coord_key = MapUtil.GetGridCoordKey(scene_index, next_position);
        if (false == FieldManager.ContainMapGrid(grid_coord_key))
        {
            return;
        }

        // tiles
        // 인접 타입을 가져오는건 메임 프레임에서 동기적으로 실행 -> static으로 하나만 들고 있어도 되려나?
        int tile_coord_key = MapUtil.GetTileCoordKey(next_position);
        if (false == FieldManager.TryGetCollisionTiles(grid_coord_key, next_position, out MapTileData[] targetTiles))
        {
            return;
        }

        // 여기서부터 Job-System 사용하도록 설정이 필요함...
        // 학습 진행 ㄱㄱ
        for (int i = 0; i < targetTiles.Length; ++i)
        {
            if (false == targetTiles[i].IsValid())
            {
                continue;
            }


        }
    }

    public void SetAnime(string key)
    {
        animator.Play(key);
    }
}
