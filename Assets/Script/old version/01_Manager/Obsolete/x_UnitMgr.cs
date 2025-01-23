//using System.Collections.Generic;
//using System.Threading.Tasks;
//using UnityEngine;


//public class UnitMgr
//{
//    private List<UnitBase>     mUnitPool;
//    private List<IUnitUpdater> mUnitUpdater;
//    private Transform          transform;        

//    public UnitMgr(Transform transform)
//    {
//        this.transform = transform;

//        mUnitPool = new List<UnitBase>();
//        mUnitUpdater = new List<IUnitUpdater>();
//    }
//    public async Task InitAsync(Transform level)
//    {
//        Debug.Log("For Test: Set Player");
//        UnitPlayer unitPlayer = await AssetMgr.SpawnUnit<UnitPlayer>(0, transform);
//        mUnitPool.Add(unitPlayer);

//        Main.Instance.SetPlayer(unitPlayer);
//        unitPlayer.Transform.position = new Vector3(0.5f, 0f, 0.5f);

//        //TODO: level 관련 처리
//        //...
//    }

//    // UnitMgr.cs -> Main.Update()에서 호출
//    public void Update()
//    {
//        for (int i = 0; i < mUnitUpdater.Count; ++i)
//        {
//            mUnitUpdater[i].Update();
//        }
//    }
//}

