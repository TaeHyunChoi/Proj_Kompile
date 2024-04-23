using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class UnitMgr
{
    private List<UnitBase> mUnitPool;

    //코딩 스탠타드를 따르면 mTransform이지만, 관용적인? transform이 더 가독성이 높으므로 예외적으로 명명
    private Transform transform;        

    public UnitMgr(Transform transform)
    {
        mUnitPool = new List<UnitBase>();
        this.transform = transform;
    }
    public async Task InitAsync(Transform level)
    {
        Debug.Log("For Test: Set Player");
        UnitPlayer unitPlayer = await AssetMgr.SpawnUnit<UnitPlayer>(0, transform);
        mUnitPool.Add(unitPlayer);

        Main.Instance.SetPlayer(unitPlayer);
        unitPlayer.Transform.position = new Vector3(0.5f, 0f, 0.5f);

        //TODO: level 관련 처리
        //...
    }

    public void Update()
    {
        //for (int i = 0; i < pool.Count; ++i)
        //{
        //    pool[i].Update();
        //}
    }
}
