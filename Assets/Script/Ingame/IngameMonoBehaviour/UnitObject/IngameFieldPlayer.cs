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

        SetAnime("Anim_Ataho_Idle_Front"); // 이게 안 먹은건가?
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

                // 여기서 CheckCollision이 맞는거 아니냐 사실
                FieldManager.CheckMove(transform.position + base.moveSpeed * Time.deltaTime * dir);
                return true;
        }

        // 오늘은 프로세스 좀 정리합시다.

        // 여기서 애니메이션도 4방향 또는 8방향으로 설정하면 된다..
        // 8방향 애니메이션 만들 수 있으면 참 좋을텐데~ AI로 구현 가능?

        return false;
    }

    private void Move(Vector3 next_position)
    {
        // grid
        //int scene_index = FieldManager.SceneIndex;
        //int grid_coord_key = MapUtil.GetGridCoordKey(scene_index, next_position);
        //if (false == FieldManager.ContainMapGrid(grid_coord_key))
        //{
        //    return;
        //}

        // tiles overlapped: unit_collider에 닿는 대상 타일 => targetTiles;
        //if (false == FieldManager.TryCheckOverlapTiles(grid_coord_key, next_position, out MapTileData[] targetTiles))
        //{
        //    return;
        //}

        // 대상 타일 내 triangles 중에서 + unit_collider와 맞닿는 triangle이 '모두' 유효해야 한다.
        //last_input_position = next_position;
        //int next_tile_key = MapUtil.GetTileCoordKey(next_position);
        //Vector3 next_tile_pivot = MapUtil.GetTilePivot(grid_coord_key, next_tile_key);
        //MapTileOverlapJobManager.Instance.ScheduleCheckOverlapTrianglesInTile(next_tile_pivot, next_position, 0.5f, targetTiles); 
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
