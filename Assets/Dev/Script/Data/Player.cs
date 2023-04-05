using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Player
{
    public struct Item
    {
        public ItemData Tbl { get; private set; }
        public int Count { get; set; }

        public Item(ItemData tbl, int count = 1)
        {
            this.Tbl = tbl;
            Count = count;
        }
    }
    public struct Skill
    { 
        public SkillData Tbl { get; private set; }

        public Skill(SkillData tbl)
        {
            this.Tbl = tbl;
        }
    }

    private static bool[] activeMember;
    public static List<Item> Items { get; set; }

    public static void Init()
    {
        activeMember = new bool[3];
        Items = new List<Item>();

        Test();

        UnitMgr.SetMyPC(Define.ATAHO);
    }

    public static void Test()
    {
        activeMember[0] = activeMember[1] = activeMember[2] = true;
        for (int i = 0; i < activeMember.Length; ++i)
        {
            if (activeMember[i])
            {
                UnitMgr.New(i, Vector3.zero);
            }
        }

        for (int i = 0; i < DataMgr.ItemTBL.Count; i++)
            Items.Add(new Item(DataMgr.ItemTBL[i], Random.Range(0, 3)));
    }
}
