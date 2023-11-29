using System;
using System.Text;
using System.Collections.Generic;


[Serializable]
public struct SkillData : Interface.IDataSetter
{
    private int index;
    private string name;
    private string info;
    private int actorCode;
    private int skillGroup;
    private int targetGroup;
    private int targetCount;
    private int power;
    private int[] mp;
    private int sp;
    private int accurate;
    private int speed;
    private string rscCode;

    public int Index { get => index; }
    public string Name { get => name; }
    public string Info { get => info; }
    public int ActorCode { get => actorCode; }
    public int SkillGroup { get => skillGroup; }
    public int TargetGroupType { get => targetGroup; }
    public int TargetCountType { get => targetCount; }
    public int Power { get => power; }
    public int[] MP { get => mp; }
    public int SP { get => sp; }
    public int Accurate { get => accurate; }
    public int Speed { get => speed; }
    public string RscCode { get => rscCode; }

    public void Set(Dictionary<string, string> data)
    {
        index = int.Parse(data["Index"]);
        name = data["Name"];
        info = data["Info"];
        actorCode = int.Parse(data["ActorCode"]);
        skillGroup = int.Parse(data["SkillGroup"]);
        skillGroup = int.Parse(data["SkillGroup"]);
        targetGroup = int.Parse(data["TargetGroup"]);
        targetCount = int.Parse(data["TargetCount"]);
        power = int.Parse(data["Power"]);

        mp = new int[4];
        mp[0] = int.Parse(data["MP00"]);
        mp[1] = int.Parse(data["MP01"]);
        mp[2] = int.Parse(data["MP02"]);
        mp[3] = int.Parse(data["MP03"]);
        sp = int.Parse(data["SP"]);

        accurate = int.Parse(data["Accurate"]);
        power = int.Parse(data["Power"]);
        speed = int.Parse(data["Speed"]);
        rscCode = data["RscCode"];
    }
}
[Serializable]
public struct ItemData : Interface.IDataSetter
{
    public byte Index { get; private set; }
    public string Name { get; private set; }
    public string Info { get; private set; }
    public byte Type { get; private set; }
    public ushort Cost { get; private set; }
    public Dictionary<short, short> Effect { get; private set; }
    public string RcsCode { get; private set; }

    public void Set(Dictionary<string, string> data)
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
[Serializable]
public struct UnitData : Interface.IDataSetter
{
    private byte code;
    private byte group;
    private string name;
    private int[] statDefault;
    private string rcsCode;

    public byte Index { get => code; }
    public byte Group { get => group; }
    public string Name { get => name; }
    public int[] StatDefault { get => statDefault; }
    public string RcsCode { get => rcsCode; }

    public void Set(Dictionary<string, string> data)
    {
        code = byte.Parse(data["Code"]);
        name = data["Name"];
        group = byte.Parse(data["Group"]);

        statDefault = new int[IDxUNIT.STAT_CNT];
        statDefault[IDxUNIT.HP] = int.Parse(data["HP"]);
        statDefault[IDxUNIT.MP] = int.Parse(data["MP"]);
        statDefault[IDxUNIT.EXP] = 0;
        statDefault[IDxUNIT.STR] = int.Parse(data["STR"]);
        statDefault[IDxUNIT.CON] = int.Parse(data["CON"]);
        statDefault[IDxUNIT.INT] = int.Parse(data["INT"]);
        statDefault[IDxUNIT.WIS] = int.Parse(data["WIS"]);
        statDefault[IDxUNIT.DEX] = int.Parse(data["DEX"]);
        statDefault[IDxUNIT.AGI] = int.Parse(data["AGI"]);
        statDefault[IDxUNIT.CHA] = int.Parse(data["CHA"]);
        statDefault[IDxUNIT.LUK] = int.Parse(data["LUK"]);

        rcsCode = data["RcsCode"];
    }
}
[Serializable]
public struct MapData : Interface.IDataSetter
{
    private ushort code;
    private ushort battleMapCode;
    private ushort[] mapNearby;
    private byte minCount;
    private byte maxCount;
    private byte[] mob;

    private string name;

    public ushort Code { get => code; }
    public string Name { get => name; }
    public ushort BattleMapCode { get => battleMapCode; }
    public byte MinCount { get => minCount; }
    public byte MaxCount { get => maxCount; }
    public ushort[] MapNearby { get => mapNearby; }
    public byte[] Mob { get => mob; }

    public void Set(Dictionary<string, string> data)
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