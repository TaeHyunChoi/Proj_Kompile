using System;

public enum EScene
{
    Opening = 0,
    Field,
}
public enum EStat
{ 
    HP = 0,
    MP,
    EXP,
    STR,
    CON,
    INT,
    WIS,
    DEX,
    AGI,
    CHA,
    LUK,
    CNT
}
public enum EAssetName
{ 
    NONE = 0,

    UnitBase,
    AnimCtrl_Ataho,
    AnimCtrl_Linxhang,
    AnimeCtrl_Smashu,

    OpeningGame,
    UITitle,
}
//public enum EAssetType
//{ 
//    None        = 0,
//    AnimCtrl    = 10000,
//    Prefab      = 20000,
//    UI          = 30000,
//}
//public enum EPrefabType
//{ 
//    None        =  0,
//    UnitBase    =  1,
//    OpeningGame =  2,
//}

//해당 자료형 값에 .ToString()하겠다는 뜻으로 접미사 ToString을 붙임
public enum EAnimeCodeToString
{ 
    NONE,

    IDLE_FRONT,
    IDLE_BACK,
    IDLE_LEFT,
    IDLE_RIGHT,

    MOVE_FRONT,
    MOVE_BACK,
    MOVE_LEFT,
    MOVE_RIGHT,
}
public enum EUIType : byte
{
    Title = 0,
    Count
}
public enum EUIGroup
{
    None = 0,

    Title,
    //title
    //title:  select

    Field,
    //content: field
    //field:  interact
    //field:  region sign

    Battle,
    //content: battle
    //battle: select

    Status,
    //status
    //stat
    //equip
    //skill
    //hud
}

//[Flags]
//public enum ETileTriggerType : ushort
//{
//    None = 0,
//    Scale = 1 << SHIFT_TRIGGER_SCALE,
//    Layer = 1 << SHIFT_TRIGGER_LAYER,
//    Event = 1 << SHIFT_TRIGGER_INTERACT
//}
//public enum ETileSizeType : byte
//{
//    Default,
//    Half,
//    Quater,
//    Inverse,
//    Default_Inverse,
//    Half_inverse,
//    Quater_inverse
//}

//[Flags]
//public enum ETileTriggerFlag : byte
//{
//    None = 0,
//    //Scale,
//    Layer,
//    Interact,
//    EventScene
//}

//public static class UtilEnum
//{
//    public static int ToInt(this EUIType type) => (int)type;
//}