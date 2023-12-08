public static class x_IDxVALUE
{
    public static float LERP = 20 * UnityEngine.Time.deltaTime;
}
public static class x_IDxSTATE
{
    //뭔가 전반적인 기획(프레임)이 흔들려서 코드도 흔들리는 기분인데 흠
    //논리 순서는 [ 입력 > 상황 > 처리 > 출력 ] 인데
    //처리 위계는 [ 상황 > 입력 > 처리 > 출력 ] 순이다.
    //그러면 Ummmmm

    public const short NONE  = 0;

    public const short FIELD = 1;

    public const short BATTLE_PLY_MENU   = 10;
    public const short BATTLE_PLY_TARGET = 11;
    public const short BATTLE_PLY_COMBO  = 12;
    public const short BATTLE_ENM_ACTION = 13;

    public const short EVENT_CUTSCENE    = 50;

    public const short SYSTEM_INFO       = 100;
    public const short SYSTEM_OPTION     = 101;
    public const short SYSTEM_CHEAT      = 102;
}
public static class x_IDxINPUT
{
    public const short NONE      = 0x00_00;

    public const short SYSTEM    = 0x0F_00;
    public const short CHEAT     = 0x02_00;
    public const short INFO      = 0x02_00;
    public const short OPTION    = 0x01_00;

    public const short DIRECTION = 0x00_F0;
    public const short UP        = 0x00_80;
    public const short DOWN      = 0x00_40;
    public const short LEFT      = 0x00_20;
    public const short RIGHT     = 0x00_10;

    public const short ACTION    = 0x00_0F;
    public const short ENTER     = 0x00_04;
    public const short CANCEL    = 0x00_02;
    public const short TRIGGER   = 0x00_01;  // joypad [LB] button 
}

public static class x_IDxUNIT
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
}
public static class x_IDxSkill
{
    public const byte BASIC   = 0;
    public const byte SOLO    = 1;
    public const byte GROUP   = 2;
    public const byte SPECIAL = 3;

    //TargetGroup
    public const byte TARGET_PARTY   = 0; //플레이어가 조작하는 유닛
    public const byte TARGET_ENEMY   = 1; //Party의 적대 유닛
    public const byte TARGET_SELF    = 2; //스킬을 시전하는 유닛
    public const byte TARGET_XORSELF = 3; //Self 이외의 Party, Enemy 모두

    //TargetCount
    public const byte TARGET_ONE = 0;
    public const byte TARGET_ALL = 1;
}
public static class x_BIT
{
    public const int MASK_NOW_TARGET     = 0x0FF0_0000;
    public const int MASK_NOW_MENU       = 0x000F_0000;
    public const int MASK_CNT_CONTENT    = 0x0000_FF00;
    public const int MASK_NOW_CONTENT    = 0x0000_00FF;

    public const int SHIFT_TARGET        = 4 * 5;
    public const int SHIFT_MENU          = 4 * 4;
    public const int SHIFT_CNT_CONTENT   = 4 * 2;
    //public static int SHIFT_CONTENT = 0; //사실상 사용X
}

public static class x_IDxUI
{
    public const byte BATTLE_BASIC   = 0;
    public const byte BATTLE_SOLO    = 1;
    public const byte BATTLE_GROUP   = 2;
    public const byte BATTLE_MODE    = 3;
    public const byte BATTLE_ITEM    = 4;
    public const byte BATTLE_SPECIAL = 5;
    public const byte BATTLE_MAX     = 6;
}