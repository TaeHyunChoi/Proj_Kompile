using System.Collections.Generic;
using UnityEngine;

public class UnitMgr
{
    private static Transform tfActive;
    private static Transform tfInactive;

    public  static Unit MyPC { get => myPC; }
    private static Unit myPC;

    private static Unit[][] unit;
    public  static Unit[] InBattle { get => unit[1]; }

    //가독성을 위한 get; set;
    private static Unit[] party  { get => unit[0]; set => unit[0] = value; }
    private static Unit[] battle { get => unit[1]; set => unit[1] = value; }
    private static Unit[] npc    { get => unit[2]; set => unit[2] = value; }

    private static Queue<int> battleOrder;


    //## Init
    public static void Init(Transform tf)
    {
        tfActive   = tf.GetChild(0);
        tfInactive = tf.GetChild(1);

        unit = new Unit[3][];
        unit[0] = new Unit[3];
        unit[1] = new Unit[7];
        unit[2] = new Unit[] { };

        battleOrder = new Queue<int>();
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


    //## Battle > Enter
    public static void BattleProc_Enter(MapData map)
    {
        BattleUnit_Init(map);
        BattleUnit_SetQueue();
    }
    private static void BattleUnit_Init(MapData map)
    {
        //## Init
        List<Unit> temp = new List<Unit>();
        Vector3 standard;
        float delta;

        //## Get Party Data => Add Battle List
        for (int i = 0; i < party.Length; ++i)
        {
            battle[i] = party[i];
            if (battle[i] != null)
            {
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
            battle[i] = New(index, Vector3.zero);
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
                battle[i].Status_SetBattle();
            }
        }
    }
    public static int Battle_WhichGroupDefeat()
    {
        bool IsPartyAllFainted = true;
        bool IsEnemyAllFainted = true;

        //[O(n)] 최대 7번이니까 더 효율적인 알고리즘은 사용 안해도 될 듯.
        for (int i = 0; i < battle.Length; ++i)
        {
            if (battle[i] == null)
            {
                continue;
            }

            if (!battle[i].IsFaint)
            {
                if (IsPartyAllFainted && battle[i].Data.Group == IDxUNIT.PARTY)
                {
                    IsPartyAllFainted = false;
                }
                else if (IsEnemyAllFainted & battle[i].Data.Group == IDxUNIT.ENEMY)
                {
                    IsEnemyAllFainted = false;
                }
            }
        }

        if (IsPartyAllFainted)
        {
            return IDxUNIT.PARTY;
        }
        else if (IsEnemyAllFainted)
        {
            return IDxUNIT.ENEMY;
        }

        return -1;
    }

    private static void BattleUnit_SetQueue()
    {
        int[] arr = new int[battle.Length];
        for (int i = 0; i < arr.Length; ++i)
        {
            arr[i] = i;
        }
        Array_OrderByQuick(ref arr, 0, arr.Length - 1);

        //## Enqueue Order In Battle
        for (int i = 0; i < arr.Length; ++i)
        {
            if (battle[arr[i]] != null && !battle[arr[i]].IsFaint)
            {
                battleOrder.Enqueue(arr[i]);
                //Debug.Log($"Pos[{arr[i]}] {battle[arr[i]].Data.Name}.Prior: {battle[arr[i]].Priority:F2}");
            }
        }
    }
    private static  void Array_OrderByQuick(ref int[] arr, int start, int end)
    {
        int idxLeft = start;
        int idxRight = end;
        float pivotPrior = Priority_Get(arr[(idxLeft + idxRight) >> 1]);

        //[내림차순] pivot 기준으로 왼쪽은 큰 수, 오른쪽은 작은 수로 정렬 (1 cycle)
        while (idxLeft < idxRight)
        {
            while (Priority_Get(arr[idxLeft]) > pivotPrior)
                ++idxLeft;
            while (Priority_Get(arr[idxRight]) < pivotPrior)
                --idxRight;

            if (idxLeft > idxRight)
                break;

            if (Priority_Get(arr[idxLeft]) != Priority_Get(arr[idxRight]))
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
        {
            Array_OrderByQuick(ref arr, start, idxLeft);
        }
        if (idxRight < end)
        {
            Array_OrderByQuick(ref arr, idxRight, end);
        }
    }
    private static float Priority_Get(int index)
    {
        Unit unit = battle[index];
        if (unit == null)
            return -1f;

        return unit.Priority;
    }

    //## Battle
    public static void Select_SetNextUnit()
    {
        //[입력] Battle Order
        int order;
        if (battleOrder.Count == 0)
        {
            BattleUnit_SetQueue();
        }
        order = battleOrder.Dequeue();
        GameMgr.Order_Update(order);

        //[처리]
        if (battle[order].Data.Group == IDxUNIT.PARTY)
        {
            GameMgr.State_Set(IDxSTATE.BATTLE_PLY_MENU);
            UIMgr.Show(IDxSTATE.BATTLE_PLY_MENU);
        }
        else
        {
            GameMgr.State_Set(IDxSTATE.NONE);
            battle[order].BattleProc_Attack();
        }
    }


    //## Battle > Get Data
    public  static int FlagTarget_Get(int index, int group)
    {
        switch (index)
        {
            case 0:     return FlagTarget_GetHPHighest(group);
            case 1:     return FlagTarget_GetHPLowest(group);
        }

        return -1;
    }
    private static int FlagTarget_GetHPHighest(int target)
    {
        int flag = 0;
        int min = (target == IDxSkill.TARGET_ENEMY) ? 3 : 0;
        int max = (target == IDxSkill.TARGET_ENEMY) ? 6 : 2;

        int saved = min;
        Unit uComp, uCurrent;
        for (int i = min + 1; i <= max; ++i)
        {
            uComp = battle[i];
            if (uComp == null || uComp.IsFaint)
            {
                continue;
            }

            uCurrent = battle[saved];
            if (uCurrent.Status[IDxUNIT.HP] < uComp.Status[IDxUNIT.HP])
            {
                saved = i;
            }
        }

        flag |= (1 << saved);
        return flag;
    }
    private static int FlagTarget_GetHPLowest(int target)
    {
        int flag = 0;
        int min = (target == IDxSkill.TARGET_ENEMY) ? 3 : 0;
        int max = (target == IDxSkill.TARGET_ENEMY) ? 6 : 2;

        int saved = min;
        Unit uComp, uCurrent;
        for (int i = min + 1; i <= max; ++i)
        {
            uComp = battle[i];
            if (uComp == null || uComp.IsFaint)
            {
                continue;
            }

            uCurrent = battle[saved];
            if (uCurrent.Status[IDxUNIT.HP] > uComp.Status[IDxUNIT.HP])
            {
                saved = i;
            }
        }

        flag |= (1 << saved);
        return flag;
    }


    //## Battle > Play Anime
    public static void Anime_PlayHit(Unit hitter, int flagTarget, int idxGroup, int idxSkill)
    {
        SkillData skill = hitter.Skill[idxGroup][idxSkill];

        for (int i = 0; i < 7; ++i)
        {
            if ((flagTarget >> i) == 1)
            {
                battle[i].BattleProc_Hit(hitter, skill);
            }
        }
    }
    public static void Anime_PlaySlow(bool slow, float lerpWeight = 1f)
    {
        float end = slow ? 0.1f : 1;
        battle[GameMgr.NowOrder].Anime_SetSpeed(end, lerpWeight);

        //for (int i = 0; i < NowActor.Targets.Count; ++i)
        //    NowActor.Targets[i].SetAnimeSpeed(end, lerpWeight);
    }
    public static void Render_SetOrder(bool isTurn)
    {
        int order = isTurn ? 2 : 0;
        battle[GameMgr.NowOrder].Render_SetOrder(order);

        //List<Unit> targets = NowActor.Targets;
        //for (int i = 0; i < targets.Count; ++i)
        //    targets[i].SetRenderOrder(order);
    }


    //## Field > Move
    public static void FieldProc_MovePlayer(int input)
    {
        //입력
        int mx = 0, mz = 0;
        if ((input & IDxINPUT.UP) != 0)
        {
            mz += 1;
        }
        if ((input & IDxINPUT.DOWN) != 0)
        {
            mz -= 1;
        }
        if ((input & IDxINPUT.RIGHT) != 0)
        {
            mx += 1;
        }
        if ((input & IDxINPUT.LEFT) != 0)
        {
            mx -= 1;
        }
        if (mx == 0 & mz == 0)
        {
            return;
        }    

        //처리
        Vector3 delta = new Vector3(mx, 0, mz) * IDxUNIT.SPEED_MOVE * Time.deltaTime;

        //출력
        CameraMgr.FollowPC(MyPC.Move(delta));
    }
}