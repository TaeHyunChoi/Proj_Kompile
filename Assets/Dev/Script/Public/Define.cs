public static class Define
{
    public const byte SPEED_MOVE        = 5;
    public const byte MAX_MENU_INDEX    = 5;

    public static string[] BattleMode = new string[] { "보통", "돌격", "방어", "선제", "반격" };
}

//## Input
public enum InputMode
{
    Block,

    Base,
    Field_Moving,
    Battle_Menu,
    Battle_Targeting,
}
public enum InputKey
{ 
    Direction   = 0xF0, //방향키 입력 확인
    Up          = 0x80,
    Down        = 0x40,
    Left        = 0x20,
    Right       = 0x10,

    Interact    = 0x0F, //상호작용 입력 확인
    Confirm     = 0x08,
    Cancel      = 0x04,
    Info        = 0x02,
    Escapce     = 0x01,

    None        = 0x00
}

//## UI
public enum UIWindow
{ 
    Battle,
}

//## Index
public static class UIIndex
{
    //다른 객체에서 같은 값을 사용할 수 있다면 class 안의 상수로 처리
    public const ushort BATTLE_MIN            = 0;
    public const ushort BATTLE_ATK_BASIC      = 1 << UIBattle.MenuShift;
    public const ushort BATTLE_ATK_SOLO       = 2 << UIBattle.MenuShift;
    public const ushort BATTLE_ATK_GROUP      = 3 << UIBattle.MenuShift;
    public const ushort BATTLE_CHANGE_MODE    = 4 << UIBattle.MenuShift;
    public const ushort BATTLE_USE_ITEM       = 5 << UIBattle.MenuShift;
    public const ushort BATTLE_ACT_SPECIAL    = 6 << UIBattle.MenuShift;
    public const ushort BATTLE_MAX            = 7 << UIBattle.MenuShift;
}
public enum StatIndex : ushort
{
    HP  = 0,      //체력   
    MP  = 1,      //기력   
    EXP = 2,      //경험치
    
    STR = 3,      //외공 공격력  (물리) 
    CON = 4,      //외공 방어력  (물리)
    INT = 5,      //내공 공격력  (마법)
    WIS = 6,      //내공 방어력  (마법)
    
    DEX = 7,      //기술력       (명중, 크리티컬)
    AGI = 8,      //순발력       (행동순서, 회피) 
    
    CHA = 9,      //카리스마     (___)
    LUK = 10,     //운           (___)
    CNT = 11
}
public enum TargetType : byte
{
    None = 0,

    Solo_Enemy = 1,
    Solo_MySelf = 2,
    Solo_Friend = 3,

    Group_Component = 4,
    Group_Friends = 5,

    All_ExceptMe = 6,
    All = 7
}
public static class AocCode
{
    public const string IDLE    = "IDLE";
    public const string MOVE    = "MOVE";
    public const string SKILL   = "SKILL";
    public const string EVENT   = "EVENT";
}
public static class UnitCode
{
    public const byte ATAHO     = 0;
    public const byte LINXHANG  = 1;
    public const byte SMASHU    = 2;
}

//## DataTable
public enum Item : ushort
{ 
    Herb            = 0x0001,
    RefreshWater, 
}
public enum Skill : ushort
{
    //## Ataho Skill
    Punch                   = 0x0001,
    Kick,
    Throw,
    Legwhip,

    Solo_Pucnch,
    Solo_Kick,
    Solo_AirKick,
    Solo_TigerSpecial,
    Solo_EnergyWave,

    Group_Dance,
    Group_Roar,
    Group_MultiWave,
    Group_RunRun,
    Group_Meteor,
}