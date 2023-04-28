using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBattleCombo : MonoBehaviour
{
    public static  UIBattleCombo Instance;
    private static UIBattleCombo instance;

    private byte direction;

    public static void Init()
    {
        if (instance != null)
            return;

        GameObject go = Resources.Load<GameObject>("Prefab/UICombo");
        go = Instantiate(go, UIMgr.Canvas_Battle.transform);
        instance = go.GetComponent<UIBattleCombo>();

        instance.gameObject.SetActive(false);
    }
    public static void Show(bool isOn)
    {
        instance.gameObject.SetActive(isOn);
        if (!isOn)
            return;

        instance.InitUI();
    }
    private void InitUI()
    {
        //여기서 최초로 스킬 아이콘을 활성화한다.

        //UnitMgr에서 현재 싸움 중인 유닛의 인덱스를 가져와
        int index = UnitMgr.Battle_GetUnit(GameMgr.NowOrder).Data.Index;

        //Player.cs에서 해당 인덱스에 저장된 콤보 스킬을 가져와라.
        byte[] combo = Player.MemberCombo[index];

        //각 방향에 Image에다가 넣어야 한다

        //위치도 설정해야 함; >> 이동 지점까지 먼저 설정해둬야 하려나?
        //이걸 어떻게 처리해야 한다?
    }

    //코루틴 쓰기 싫어서 LateUpdate 쓴다...
    private void LateUpdate()
    {
        //isOn == true
        //상하좌우 UI슬롯이 밝아지며 커진다. (위 작업이 끝나면 LateUpdate()를 할 필요가 없어지는군요
        //상하좌우 UI슬롯에 콤보 스킬 아이콘을 입력한다

        //isOn == false;
        //상하좌우 UI슬롯이 어두워지며 작아진다.

        //추가 조작
        //IDxInput.Direction => GetButtonDown() ? 해당 UI 슬롯 테두리 밝게
        //확인키(스페이스 또는 Z)를 누르고 있으면서 + Direction.ButtonUp()을 해야 스킬 발동
    }
}
