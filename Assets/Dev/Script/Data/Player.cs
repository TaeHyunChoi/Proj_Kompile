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

    public static List<SkillData> Skills { get; private set; }
    public static List<Item> Items { get; set; }

    public static void TempItem()
    {
        Items = new List<Item>();
        for (int i = 0; i < DataMgr.ItemTBL.Count; i++)
            Items.Add(new Item(DataMgr.ItemTBL[i], Random.Range(0, 3)));
    }
}
