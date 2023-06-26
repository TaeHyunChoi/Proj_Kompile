//## Index
public static class IDxUNIT
{
    public const byte SPEED_MOVE = 5;

    //Unit Index
    public const byte ATAHO     = 0;
    public const byte LINXHANG  = 1;
    public const byte SMASHU    = 2;
    public const byte COMMON    = 255;

    //Group Index
    public const byte PARTY     = 0;
    public const byte ENEMY     = 1;
    public const byte NPC       = 2;

    //Stat
    public const byte HP        = 0;     //체력   
    public const byte MP        = 1;     //기력   
    public const byte EXP       = 2;     //경험치
    public const byte STR       = 3;     //물리 공격력
    public const byte CON       = 4;     //물리 방어력
    public const byte INT       = 5;     //마법 공격력
    public const byte WIS       = 6;     //마법 방어력
    public const byte DEX       = 7;     //기술력       (명중, 크리티컬)
    public const byte AGI       = 8;     //순발력       (행동순서, 회피) 
    public const byte CHA       = 9;     //카리스마     (___)
    public const byte LUK       = 10;    //운           (___)
    public const byte STAT_CNT  = 11;

    //Battle Mode
    public const byte MODE_NORMAL       = 0;
    public const byte MODE_CHARGE       = 1;
    public const byte MODE_DEFENCE      = 2;
    public const byte MODE_PREEMTIVE    = 3;
    public const byte MODE_COUNTER      = 4;
    public const byte MODE_COUNT        = 5;

    //Animation
    public const string ANIME_IDLE    = "IDLE";
    public const string ANIME_MOVE    = "MOVE";
    public const string ANIME_SKILL   = "SKILL";
    public const string ANIME_HIT     = "HIT";
    public const string ANIME_EVENT   = "EVENT";

    //[Unit.Group]_[Target Count]
    public const byte TARGET_PRT_SOLO   = 0; // Unit Type : Party One
    public const byte TARGET_PRT_ALL    = 1; // Unit Type : Party All
    public const byte TARGET_ENM_SOLO   = 2; // Unit Type : Enemy One
    public const byte TARGET_ENM_ALL    = 3; // Unit Type : Enemy All
    public const byte TARGET_SELF       = 4; // 본인
    public const byte TARGET_XOR_SELF   = 5; // 본인을 제외한 모든 유닛
    public const byte TARGET_ALL        = 6; // 피아식별X
}
public static class IDxINPUT
{
    //None
    public const byte NONE = 0x00;

    //Mode
    public const byte BASE             =  1;
    public const byte FIELD            =  2;
    public const byte BATTLE_MENU      =  3;
    public const byte BATTLE_TARGERT   =  4;
    public const byte BATTLE_COMBO     =  5;
    public const byte CHEAT            = 15;

    //Direction
    public const byte DIRECTION      = 0xF0;
    public const byte UP             = 0x80;
    public const byte DOWN           = 0x40;
    public const byte LEFT           = 0x20;
    public const byte RIGHT          = 0x10;

    //Interact
    public const byte INTERACT       = 0x0F;
    public const byte ENTER          = 0x08;
    public const byte CANCEL         = 0x04;
    public const byte OPTION         = 0x02;  // System Option
    public const byte TRIGGER        = 0x01;  // joypad [LB] button 
}
public static class IDxUI
{
    //UI Window
    public const byte BATTLE_MENU = 0;
    public const byte BATTLE_TARGET = 1;
    public const byte BATTLE_COMBO  = 2;
}
public static class IDxSkill
{
    public const byte BASIC   = 0;
    public const byte SOLO    = 1;
    public const byte GROUP   = 2;
    public const byte SPECIAL = 3;

    public const byte TARGET_PARTY = 0;
    public const byte TARGET_ENEMY = 1;
    public const byte TARGET_SELF = 2;
    public const byte TARGET_EXCEPT_SELF = 3;
}
public static class IDxVALUE
{
    public static float LERP = 20 * UnityEngine.Time.deltaTime;
}

//## Bit Masking
public static class BIT
{
    public const int MASK_NOW_TARGET     = 0x0FF0_0000;
    public const int MASK_NOW_MENU       = 0x000F_0000;
    public const int MASK_CNT_CONTENT    = 0x0000_FF00;
    public const int MASK_NOW_CONTENT    = 0x0000_00FF;

    public const int SHIFT_TARGET        = 4 * 5;
    public const int SHIFT_MENU          = 4 * 4;
    public const int SHIFT_CONTENT_LAST  = 4 * 2;
    //public static int SHIFT_CONTENT = 0; //사실상 사용X
}

public enum ETargetGroup
{ 
    Party = 0,  //플레이어가 조작하는 유닛
    Enemy,      //Party의 적대 유닛
    Self,       //스킬을 시전하는 유닛
    ExceptSelf  //Self 이외의 Party, Enemy 모두
}
public enum ETargetCount
{
    One = 0,
    All
}
public enum EIdxMENU : int
{
    SkillBasic = 0,
    SkillSolo,
    SkillGroup,
    Mode,
    Item,
    SkillSpecial,
    Count
}