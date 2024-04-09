using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class UnitMgr
{
    private List<UnitBase> pool;
    private Transform transform;


    public UnitMgr(Transform transform)
    {
        pool = new List<UnitBase>();
        this.transform = transform;
    }
    public async Task InitAsync(Transform level)
    {
        GameObject obj = await AssetManager.InstantiateAsync("UnitBase", transform, true);
        UnitPlayer player = obj.AddComponent<UnitPlayer>();
        player.transform.position = new Vector3(0.5f, 0f, 0.5f);
        Main.Instance.SetPlayer(player);
        Main.Cam.SetFollow(player.transform);

        pool.Add(player);

        //UnitBase[] npc = level.GetComponentsInChildren<UnitBase>(true);
    }
}
