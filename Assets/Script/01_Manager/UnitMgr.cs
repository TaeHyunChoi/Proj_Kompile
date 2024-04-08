using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class UnitMgr
{
    private List<Unit> pool;
    
    private Transform transform;


    public Unit Player { get => pool[0]; }

    public UnitMgr(Transform transform)
    {
        pool = new List<Unit>();
        this.transform = transform;
    }
    public async Task InitAsync(Transform level)
    {
        GameObject obj = await AssetManager.InstantiateAsync("UnitBase", transform, true);
        Unit player = obj.GetComponent<Unit>();
        player.transform.position = new Vector3(0.5f, 0f, 0.5f);
        Main.Camera.SetFollow(player.transform);

        pool.Add(player);


        Unit[] npc = level.GetComponentsInChildren<Unit>(true);
    }
}
