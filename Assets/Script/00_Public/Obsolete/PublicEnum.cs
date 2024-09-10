using System;
using static Index.IDxTile;

// enum
public enum ESceneState : byte
{ 
    None,
    Load,
    Play,
    Pause,
    Leave,
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

public enum EAsset
{ 
    NONE = 0,

    UnitBase,
    AnimCtrl_Ataho,
    AnimCtrl_Linxhang,
    AnimeCtrl_Smashu,

    OpeningGame,
    UITitle,
}

public enum EAssetType
{ 
    None        = 0,
    AnimCtrl    = 10000,
    Prefab      = 20000,
    UI          = 30000,
}
public enum EPrefabType
{ 
    None        =  0,
    UnitBase    =  1,
    OpeningGame =  2,
}

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
[Flags]
public enum EGameStateFlag : byte
{ 
    None    = 0,
    Opening = 1 << 4,
    Field   = 1 << 5,
    Battle  = 1 << 6,
    Event   = 1 << 7
}
public enum EUIType : byte
{
    Title = 0,
}

[Flags]
public enum ETileTriggerType : ushort
{
    None = 0,
    Scale = 1 << SHIFT_TRIGGER_SCALE,
    Layer = 1 << SHIFT_TRIGGER_LAYER,
    Event = 1 << SHIFT_TRIGGER_INTERACT
}
public enum ETileSizeType : byte
{
    Default,
    Half,
    Quater,
    Inverse,
    Default_Inverse,
    Half_inverse,
    Quater_inverse
}