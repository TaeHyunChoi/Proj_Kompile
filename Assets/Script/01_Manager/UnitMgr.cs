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
        Debug.Log("For Test: Set Player");
        UnitPlayer player = await AssetMgr.SpawnUnit<UnitPlayer>(0, transform);
        player.transform.position = new Vector3(0.5f, 0f, 0.5f);
        Main.Instance.SetPlayer(player);
        pool.Add(player);

        //UnitBase[] npc = level.GetComponentsInChildren<UnitBase>(true);
        //Set npc...
    }
}
