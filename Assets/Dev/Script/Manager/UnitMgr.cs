using System.Collections.Generic;
using UnityEngine;

public class UnitMgr
{
    private static Transform tfActive;
    private static Transform tfInactive;

    public static  Unit[] Party { get => party; }
    private static Unit[] party = new Unit[3];

    public  static Unit MyPC { get => myPC; }
    private static Unit myPC;

    private static UnitMgr_Battle battle;
    private static UnitMgr_Field  field;

    public static void Init(Transform tf)
    {
        tfActive = tf.GetChild(0);
        tfInactive = tf.GetChild(1);

        battle = new UnitMgr_Battle();
        field  = new UnitMgr_Field();
    }
    public static Unit New(int unitCode, Vector3 pos)
    {
        Unit newUnit;
        if (tfInactive.childCount > 0)
        {
            newUnit = tfInactive.GetChild(0).GetComponent<Unit>();
        }
        else
        {
            GameObject go = ResourceMgr.Prefab["UnitBase"];
            go = GameObject.Instantiate(go, tfActive);
            newUnit = go.GetComponent<Unit>();
        }

        newUnit.Init(unitCode);
        newUnit.transform.position = pos;
        newUnit.transform.eulerAngles = new Vector3(50, 0, 0);
        newUnit.gameObject.name = newUnit.Data.RcsCode;

        return newUnit;
    }
    public static void Test_SetMyPC()
    {
        party[IDxUNIT.ATAHO]    = New(IDxUNIT.ATAHO, Vector3.zero);
        party[IDxUNIT.LINXHANG] = New(IDxUNIT.LINXHANG, Vector3.zero);
        party[IDxUNIT.SMASHU]   = New(IDxUNIT.SMASHU, Vector3.zero);

        myPC = party[IDxUNIT.ATAHO];
    }
}
public class UnitMgr_Battle
{
    public static  Unit[] InBattle { get => battle; }
    private static Unit[] battle = new Unit[7];
    private static Queue<int> battleOrder = new Queue<int>();
    public static Unit NowActor { get => battle[GameMgr.NowOrder]; } //없애고 싶은 친구

    //## Battle > Set Battle Situation
    public static  void ProcEnter(MapData map)
    {
        //## Init
        List<Unit> temp = new List<Unit>();
        Vector3 standard;
        float delta;

        //## Get Party Data => Add Battle List
        Unit[] party = UnitMgr.Party;
        for (int i = 0; i < party.Length; ++i)
        {
            if (party[i] != null)
            {
                battle[i] = party[i];
                temp.Add(battle[i]);
            }
        }

        //## Set Party Position
        standard = new Vector3(-4f, 100f, -3.3f);
        switch (temp.Count)
        {
            case 1:
                delta = -1.5f;
                temp[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                break;
            case 2:
                delta = -1f;
                temp[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                temp[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
            case 3:
                delta = -1.5f;
                temp[0].transform.localPosition = standard;
                temp[1].transform.localPosition = standard + new Vector3(0, 0, delta);
                temp[2].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
        }

        //## Set Enemy Data => Add Battle List
        temp.Clear();
        int count = Random.Range(map.MinCount, map.MaxCount + 1); //맵 Mob 개수
        int variety = map.Mob.Length;

        byte index;
        for (int i = 3; i < count + 3; ++i)
        {
            index = map.Mob[Random.Range(0, variety)];
            battle[i] = UnitMgr.New(index, Vector3.zero);
            temp.Add(battle[i]);
        }

        //## Set Enemy Position
        standard = new Vector3(4.5f, 100f, -3f);
        switch (temp.Count)
        {
            case 1:
                delta = -1.325f;
                temp[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                break;
            case 2:
                delta = -1.25f;
                temp[0].transform.localPosition = standard + new Vector3(0, 0, delta);
                temp[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                break;
            case 3:
                delta = -1.25f;
                temp[0].transform.localPosition = standard + new Vector3(-0.5f, 0, delta);
                temp[1].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                temp[2].transform.localPosition = standard + new Vector3(-0.5f, 0, delta * 3);
                break;
            case 4:
                delta = -1.25f;
                temp[0].transform.localPosition = standard + new Vector3(-0.5f, 0, 0);
                temp[1].transform.localPosition = standard + new Vector3(0, 0, delta);
                temp[2].transform.localPosition = standard + new Vector3(0, 0, delta * 2);
                temp[3].transform.localPosition = standard + new Vector3(-1f, 0, delta * 3);
                break;
        }

        //## Set Battle Unit`s Stats
        for (int i = 0; i < battle.Length; ++i)
        {
            if (battle[i] != null)
            {
                battle[i].Battle_SetStatus();
            }
        }

        //## Set Order
        int[] arr = new int[battle.Length];
        for (int i = 0; i < arr.Length; ++i)
        {
            arr[i] = i;
        }
        OrderByQuickSort(ref arr, 0, arr.Length - 1);

        //## Enqueue Order In Battle
        for (int i = 0; i < arr.Length; ++i)
        {
            if (battle[arr[i]] != null)
            {
                battleOrder.Enqueue(arr[i]);
                Debug.Log($"Pos[{arr[i]}] {battle[arr[i]].Data.Name}.Prior: {battle[arr[i]].Priority:F2}");
            }
        }
    }
    private static void OrderByQuickSort(ref int[] arr, int start, int end)
    {
        int idxLeft = start;
        int idxRight = end;
        float pivotPrior = GetPrior(arr[(idxLeft + idxRight) >> 1]);

        //[내림차순] pivot 기준으로 왼쪽은 큰 수, 오른쪽은 작은 수로 정렬 (1 cycle)
        while (idxLeft < idxRight)
        {
            while (GetPrior(arr[idxLeft]) > pivotPrior)
                ++idxLeft;
            while (GetPrior(arr[idxRight]) < pivotPrior)
                --idxRight;

            if (idxLeft > idxRight)
                break;

            if (GetPrior(arr[idxLeft]) != GetPrior(arr[idxRight]))
            {
                arr[idxLeft] ^= arr[idxRight];
                arr[idxRight] ^= arr[idxLeft];
                arr[idxLeft] ^= arr[idxRight];
            }

            ++idxLeft;
            --idxRight;
        }

        //left, right 위치 역전됨 → 가독성을 위해 Swap(left, right)
        if (idxLeft != idxRight)
        {
            idxLeft ^= idxRight;
            idxRight ^= idxLeft;
            idxLeft ^= idxRight;
        }

        if (start < idxLeft)
            OrderByQuickSort(ref arr, start, idxLeft);
        if (idxRight < end)
            OrderByQuickSort(ref arr, idxRight, end);
    }
    private static float GetPrior(int index)
    {
        Unit unit = battle[index];
        if (unit == null)
            return -1f;

        return unit.Priority;
    }


    //## Battle > Unit Data (for Action)
    public static void SelectAction()
    {
        int order = battleOrder.Dequeue();
        GameMgr.UpdateOrder(order);

        if (battle[order].Data.Group == IDxUNIT.PARTY)
        {
            UIMgr.Show(IDxUI.BATTLE_MENU, true);
        }
        else
        {
            InputMgr.SetMode(IDxINPUT.BASE);
            battle[order].ProcBattle_AI();
        }
    }
    public static void SetRenderOrder(bool isTurn)
    {
        int order = isTurn ? 2 : 0;
        NowActor.SetRenderOrder(order);

        //List<Unit> targets = NowActor.Targets;
        //for (int i = 0; i < targets.Count; ++i)
        //    targets[i].SetRenderOrder(order);
    }
    public static void SlowUnitAnime(bool slow, float lerpWeight = 1f)
    {
        float end = slow ? 0.1f : 1;

        NowActor.SetAnimeSpeed(end, lerpWeight);
        //for (int i = 0; i < NowActor.Targets.Count; ++i)
        //    NowActor.Targets[i].SetAnimeSpeed(end, lerpWeight);
    }

}
public class UnitMgr_Field
{
    private static List<Unit> npc = new List<Unit>();
    public static void Field_PlayerMoveTo(int input)
    {
        int mx = 0, mz = 0;

        if ((input & IDxINPUT.UP) != 0)
            mz += 1;
        if ((input & IDxINPUT.DOWN) != 0)
            mz -= 1;
        if ((input & IDxINPUT.RIGHT) != 0)
            mx += 1;
        if ((input & IDxINPUT.LEFT) != 0)
            mx -= 1;

        Vector3 move = MyPC.MoveTo(new Vector3(mx, 0, mz));
        CameraMgr.FollowPC(move);
    }
}