using System.Collections.Generic;
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

    public static byte[][] MemberCombo { get => memberCombos; }
    private static byte[][] memberCombos;

    public static List<Item> Items { get; set; }

    public static void Init()
    {
        Items = new List<Item>();
        
        activeMember = new bool[3];
        memberCombos = new byte[3][];
        for (int i = 0; i < memberCombos.Length; ++i)
            memberCombos[i] = new byte[4];
    }


    public static void Test()
    {
        activeMember[0] = true;
        activeMember[1] = false;
        activeMember[2] = false;
        for (int i = 0; i < activeMember.Length; ++i)
        {
            if (activeMember[i])
                UnitMgr.New(i, Vector3.zero);
        }
        memberCombos[IDxUNIT.ATAHO][0] = 1; //돌려차기
        memberCombos[IDxUNIT.ATAHO][1] = 4; //호격권

        UnitMgr.Test_SetMyPC();

        for (int i = 0; i < DataMgr.ItemTBL.Count; i++)
            Items.Add(new Item(DataMgr.ItemTBL[i], Random.Range(0, 3)));
    }
}