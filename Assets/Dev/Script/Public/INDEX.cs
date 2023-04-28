
public class IDxUNIT
{
    public const byte SPEED_MOVE = 5;

    //Unit Index
    public const byte ATAHO     = 0;
    public const byte LINXHANG  = 1;
    public const byte SMASHU    = 2;
    public const byte COMMON    = 255;

    //Group Index
    public const byte PLAYER    = 1;
    public const byte ENEMY     = 2;
    public const byte NPC       = 3;

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

    //Animation
    public const string ANIME_IDLE    = "IDLE";
    public const string ANIME_MOVE    = "MOVE";
    public const string ANIME_SKILL   = "SKILL";
    public const string ANIME_HIT     = "HIT";
    public const string ANIME_EVENT   = "EVENT";

    //[Unit.Group]_[Target Count]
    public const byte TARGET_ENM_SOLO   = 1;
    public const byte TARGET_SELF       = 2;
    public const byte TARGET_PLY_SOLO   = 3;
    public const byte TARGET_ENM_ALL    = 4;
    public const byte TARGET_PLY_ALL    = 5;
    public const byte TARGET_XOR_SELF   = 6;
}
public class IDxINPUT
{
    //Mode
    public const byte MODE_BLOCKED        = 0;
    public const byte MODE_BASE           = 1;
    public const byte MODE_FIELD          = 2;
    public const byte MODE_BATTLE_MENU    = 3;
    public const byte MODE_BATTLE_TARGERT = 4;
    public const byte MODE_CHEAT          = 7;

    //Direction
    public const int DIRECTION      = 0xF0;
    public const int UP             = 0x80;
    public const int DOWN           = 0x40;
    public const int LEFT           = 0x20;
    public const int RIGHT          = 0x10;

    //Interact
    public const int INTERACT       = 0x0F;
    public const int ENTER          = 0x08;
    public const int CANCEL         = 0x04;
    public const int INFO           = 0x02;
    public const int ESCAPE         = 0x01;
    public const int NONE           = 0x00;
}
public class IDxUI
{
    //UI Window
    public const byte WND_BATTLE        = 0x00;
}
public class IDxSkill
{
    public const byte BASIC   = 1;
    public const byte SOLO    = 2;
    public const byte GROUP   = 3;
    public const byte SPECIAL = 6;
}