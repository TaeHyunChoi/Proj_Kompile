namespace DataStruct
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using UnityEngine;
    using CMathf;
    using static Index.IDxTile;

    [Serializable]
    public struct STile
    {
        //total 10 bytes
        private byte   maskInfo;
        private ushort maskTrigger;
        private ulong  maskMove;

        public bool IsMovable(int indexTriangle)
        {
            int mask = (int)(maskMove & 0xFFFF);
            mask &= (1 << indexTriangle);
            return 0 != mask;
        }
        public bool HasTrigger(ETileTriggerType type, out int value)
        {
            value = 0;
            bool hasTrigger = (0 != (maskTrigger & (ushort)type));

            switch (type)
            {
                case ETileTriggerType.Scale:
                    value = (int)((maskTrigger >> SHIFT_TRIGGER_SCALE_VALUE) & 0b_0001);
                    break;
                case ETileTriggerType.Layer:
                    value = (int)((maskTrigger >> SHIFT_TRIGGER_LAYER_VALUE) & 0b_1111);
                    break;
                case ETileTriggerType.Event:
                    value = (int)((maskTrigger >> SHIFT_TRIGGER_INTERACT_VALUE) & 0b_0011_1111_1111);
                    break;
            }

            return hasTrigger;
        }
        public float GetScale(ETileSizeType type = ETileSizeType.Default)
        {
            float scale = (0 != (maskInfo >> SHIFT_INFO_SCALE)) ? 0.5f : 1f;
            return TileUtility.GetScale(type, scale);
        }
        public float GetYValue(int keyMy, Vector3 point)
        {
            //point가 속한 삼각형 인덱스를 구한다.
            Vector3 pivot = TileUtility.GetPivotByKey(keyMy, GetScale());
            float scale_half = GetScale(ETileSizeType.Half);
            int triangle = TileUtility.GetTriangleIndex(point - pivot, scale_half);

            //각 포인트에 해당하는 높이 구한다. (배열을 사용하지 않기 위해 out 3번 사용...)
            //[주의!] 유니티는 "왼손 좌표계"이므로 외적 계산을 반대로 생각해야 한다...
            long height = (long)(maskMove >> 16);
            TileUtility.GetTrianglePoints(pivot, GetScale(), height, triangle, out Vector3 p0, out Vector3 p1, out Vector3 p2);

            //평면의 방정식에 대입하면 y값을 구할 수 있다.
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            normal.Normalize();
            normal = CMath.FloorToVector(normal, 3);
            float d = Vector3.Dot(normal, p0);

            return -(normal.x * point.x + normal.z * point.z - d) / normal.y;
        }

        //Only for Tile Map Sampling
#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN
        public int Info { get => maskInfo; }
        public int Move { get => (int)(maskMove & 0xFFFF); }
        public long Height { get => (long)(maskMove >> 16); }
        public int Trigger { get => maskTrigger; }
        public STile(int info, int trigger, int move, long height)
        {
            maskInfo = (byte)info;
            maskTrigger = (ushort)trigger;
            maskMove = (ulong)((height << 16) | (long)move);
        }
#endif
    }

    //아래 자료구조는 미완성. 개발 현황에 따라 내용이 달라질 예정
    [Serializable]
    public struct SkillData : IDataSetter
    {
        public int    Index { get; private set; }
        public string Name { get; private set; }
        public string Info { get; private set; }
        public int    ActorCode { get; private set; }
        public int    SkillGroup { get; private set; }
        public int    TargetGroupType { get; private set; }
        public int    TargetCountType { get; private set; }
        public int    Power { get; private set; }
        public int[]  MP { get; private set; }
        public int    SP { get; private set; }
        public int    Accurate { get; private set; }
        public int    Speed { get; private set; }
        public string RscCode { get; private set; }

        public void Set(Dictionary<string, string> data)
        {
            Index = int.Parse(data["Index"]);
            Name = data["Name"];
            Info = data["Info"];
            ActorCode = int.Parse(data["ActorCode"]);
            SkillGroup = int.Parse(data["SkillGroup"]);
            TargetGroupType = int.Parse(data["TargetGroup"]);
            TargetCountType = int.Parse(data["TargetCount"]);
            Power = int.Parse(data["Power"]);
            MP = new int[4];
            MP[0] = int.Parse(data["MP00"]);
            MP[1] = int.Parse(data["MP01"]);
            MP[2] = int.Parse(data["MP02"]);
            MP[3] = int.Parse(data["MP03"]);
            SP = int.Parse(data["SP"]);
            Accurate = int.Parse(data["Accurate"]);
            Speed = int.Parse(data["Speed"]);
            RscCode = data["RscCode"];
        }
    }
    [Serializable]
    public struct ItemData : IDataSetter
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
    public struct UnitData : IDataSetter
    {
        public byte Code { get; private set; }
        public string Name { get; private set; }
        public byte Group { get; private set; }
        public int[] StatDefault { get; private set; }
        public string RcsCode { get; private set; }

        public void Set(Dictionary<string, string> data)
        {
            Code = byte.Parse(data["Code"]);
            Name = data["Name"];
            Group = byte.Parse(data["Group"]);
            StatDefault = new int[(int)EStat.CNT];
            StatDefault[(int)EStat.HP] = int.Parse(data["HP"]);
            StatDefault[(int)EStat.MP] = int.Parse(data["MP"]);
            StatDefault[(int)EStat.EXP] = 0;
            StatDefault[(int)EStat.STR] = int.Parse(data["STR"]);
            StatDefault[(int)EStat.CON] = int.Parse(data["CON"]);
            StatDefault[(int)EStat.INT] = int.Parse(data["INT"]);
            StatDefault[(int)EStat.WIS] = int.Parse(data["WIS"]);
            StatDefault[(int)EStat.DEX] = int.Parse(data["DEX"]);
            StatDefault[(int)EStat.AGI] = int.Parse(data["AGI"]);
            StatDefault[(int)EStat.CHA] = int.Parse(data["CHA"]);
            StatDefault[(int)EStat.LUK] = int.Parse(data["LUK"]);
            RcsCode = data["RcsCode"];
        }
    }
    [Serializable]
    public struct MapData : IDataSetter
    {
        public ushort Code { get; private set; }
        public string Name { get; private set; }
        public ushort BattleMapCode { get; private set; }
        public byte MinCount { get; private set; }
        public byte MaxCount { get; private set; }
        public ushort[] MapNearby { get; private set; }
        public byte[] Mob { get; private set; }

        public void Set(Dictionary<string, string> data)
        {
            Code = ushort.Parse(data["Code"]);
            Name = data["Name"];
            BattleMapCode = ushort.Parse(data["BattleMapCode"]);
            MinCount = byte.Parse(data["MinCount"]);
            MaxCount = byte.Parse(data["MaxCount"]);

            StringBuilder builder = new StringBuilder();
            builder.Append("Nearby");
            MapNearby = new ushort[4];
            for (int i = 0; i < MapNearby.Length; ++i)
            {
                builder.Append(i);
                MapNearby[i] = ushort.Parse(data[builder.ToString()]);
                builder.Remove(builder.Length - 1, 1);
            }
            builder.Clear();

            List<byte> temp = new List<byte>();
            builder.Append("Mob");
            for (int i = 0; i < 10; ++i)
            {
                builder.Append(i);
                byte mobCode = byte.Parse(data[builder.ToString()]);
                if (mobCode <= 0)
                    break;

                //Mob[i] = byte.Parse(data[sb.ToString()]);
                temp.Add(mobCode);
                builder.Remove(builder.Length - 1, 1);
            }
            Mob = temp.ToArray();
        }
    }

}