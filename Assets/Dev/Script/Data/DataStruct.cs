using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public struct SkillData : Interface.IDataSetter
{
    //데이터 패킹을 하면 '아주 적게나마' 메모리를 아끼는 셈이로구나.

    private string name;
    private string info;
    private string rcsCode;

    private int indexPack;  //actorIndex, skillGroup, targetGroup, afterBuffIndex
    private int specPack;   //accurate, power, speed

    public string Name { get => name; }
    public string Info { get => info; }
    public string RcsCode { get => rcsCode; }

    public int Index        { get => (indexPack & 0x0000_00FF); }
    public int ActorIndex   { get => (indexPack & 0x0000_0F00) >> (4 * 2); }
    public int SkillGroup   { get => (indexPack & 0x0000_F000) >> (4 * 3); }
    public int TargetGroup  { get => (indexPack & 0x000F_0000) >> (4 * 4); }
    public int BuffIndex    { get => (indexPack & 0x0FF0_0000) >> (4 * 5); }

    public int Accurate     { get => (specPack & 0x0000_FFFF); }
    public int Speed        { get => (specPack & 0x000F_0000) >> (4 * 4); }
    public int Power        { get => (specPack & 0x00F0_0000) >> (4 * 5); }

    public void SetTable(Dictionary<string, string> data)
    {
        name = data["Name"];
        info = data["Info"];
        rcsCode = data["RscCode"];

        indexPack |= byte.Parse(data["ActorIndex"]) << (4 * 2);
        indexPack |= byte.Parse(data["SkillGroup"]) << (4 * 3);
        indexPack |= byte.Parse(data["TargetGroup"]) << (4 * 4);
        indexPack |= byte.Parse(data["AfterBuffIndex"]) << (4 * 5);

        specPack |= ushort.Parse(data["Accurate"]);
        specPack |= byte.Parse(data["Speed"]) << (4 * 4);
        specPack |= byte.Parse(data["Power"]) << (4 * 5);
    }
}
public struct ItemData : Interface.IDataSetter
{
    public byte Index { get; private set; }
    public string Name { get; private set; }
    public string Info { get; private set; }
    public byte Type { get; private set; }
    public ushort Cost { get; private set; }
    public Dictionary<short, short> Effect { get; private set; }
    public string RcsCode { get; private set; }

    public void SetTable(Dictionary<string, string> data)
    {
        Index = byte.Parse(data["Index"]);
        Name = data["Name"];
        Info = data["Info"];
        Type = byte.Parse(data["Type"]);
        Cost = ushort.Parse(data["Cost"]);

        Effect = new Dictionary<short, short>();
        Effect.Add(short.Parse(data["Effect00"]), short.Parse(data["Effect00Value"]));
        Effect.Add(short.Parse(data["Effect01"]), short.Parse(data["Effect01Value"]));
        RcsCode = data["RcsCode"];
    }
}
public struct UnitData : Interface.IDataSetter
{
    public byte Code { get; private set; }
    public byte Group { get; private set; }
    public string Name { get; private set; }
    public ushort[] StatDefault { get; private set; }
    public string RcsCode { get; private set; }

    public void SetTable(Dictionary<string, string> data)
    {
        Code = byte.Parse(data["Code"]);

        if (Code < 3)
            Group = UnitMgr.GROUP_PLY;
        else if (Code < 200)
            Group = UnitMgr.GROUP_ENM;
        else
            Group = UnitMgr.GROUP_NPC;

        Name = data["Name"];

        StatDefault = new ushort[(byte)StatIndex.CNT];
        StatDefault[(byte)StatIndex.HP] = ushort.Parse(data["HP"]);
        StatDefault[(byte)StatIndex.MP] = ushort.Parse(data["MP"]);
        StatDefault[(byte)StatIndex.EXP] = 0;
        StatDefault[(byte)StatIndex.STR] = ushort.Parse(data["STR"]);
        StatDefault[(byte)StatIndex.CON] = ushort.Parse(data["CON"]);
        StatDefault[(byte)StatIndex.INT] = ushort.Parse(data["INT"]);
        StatDefault[(byte)StatIndex.WIS] = ushort.Parse(data["WIS"]);
        StatDefault[(byte)StatIndex.DEX] = ushort.Parse(data["DEX"]);
        StatDefault[(byte)StatIndex.AGI] = ushort.Parse(data["AGI"]);
        StatDefault[(byte)StatIndex.CHA] = ushort.Parse(data["CHA"]);
        StatDefault[(byte)StatIndex.LUK] = ushort.Parse(data["LUK"]);

        RcsCode = data["RcsCode"];
    }
}
public struct MapData : Interface.IDataSetter
{
    private ushort code;
    private string name;
    private ushort battleMapCode;
    private byte minCount;
    private byte maxCount;
    private ushort[] mapNearby;
    private byte[] mob;

    public ushort Code { get => code; }
    public string Name { get => name; }
    public ushort BattleMapCode { get => battleMapCode; }
    public byte MinCount { get => minCount; }
    public byte MaxCount { get => maxCount; }
    public ushort[] MapNearby { get => mapNearby; }
    public byte[] Mob { get => mob; }

    public void SetTable(Dictionary<string, string> data)
    {
        code = ushort.Parse(data["Code"]);
        name = data["Name"];
        battleMapCode = ushort.Parse(data["BattleMapCode"]);
        minCount = byte.Parse(data["MinCount"]);
        maxCount = byte.Parse(data["MaxCount"]);

        StringBuilder sb = new StringBuilder();
        sb.Append("Nearby");
        mapNearby = new ushort[4];
        for (int i = 0; i < MapNearby.Length; ++i)
        {
            sb.Append(i);
            MapNearby[i] = ushort.Parse(data[sb.ToString()]);
            sb.Remove(sb.Length - 1, 1);
        }
        sb.Clear();

        List<byte> temp = new List<byte>();
        sb.Append("Mob");
        for (int i = 0; i < 10; ++i)
        {
            sb.Append(i);
            byte mobCode = byte.Parse(data[sb.ToString()]);
            if (mobCode <= 0)
                break;

            //Mob[i] = byte.Parse(data[sb.ToString()]);
            temp.Add(mobCode);
            sb.Remove(sb.Length - 1, 1);
        }
        mob = temp.ToArray();
    }
}