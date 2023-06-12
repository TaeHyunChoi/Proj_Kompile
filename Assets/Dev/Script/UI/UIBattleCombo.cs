using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIBattleCombo : MonoBehaviour
{
    public static  UIBattleCombo Instance { get => instance; }
    private static UIBattleCombo instance;

    private RectTransform rect;

    private Image dimBlack;
    private Image[] iconSlots;
    private RectTransform[] rectSlots;

    private Vector3     center;
    private Vector3[]   endPos;

    private byte direction;

    public static void Init()
    {
        if (instance != null)
            return;

        GameObject go = Resources.Load<GameObject>("Prefab/UIBattleCombo");
        go = Instantiate(go, UIMgr.Canvas_Battle.transform);
        instance = go.GetComponent<UIBattleCombo>();
        instance.AwakeManually();

        instance.gameObject.SetActive(false);
    }
    private void AwakeManually()
    {
        rect = transform.GetComponent<RectTransform>();
        iconSlots = new Image[4];
        rectSlots = new RectTransform[4];
        endPos = new Vector3[4];

        Color color;
        dimBlack = rect.GetChild(0).GetComponent<Image>();
        color = dimBlack.color;
        dimBlack.color = new Color(color.r, color.g, color.b, 0);

        for (int i = 1; i < rect.childCount; ++i)
        {
            iconSlots[i - 1] = rect.GetChild(i).GetComponent<Image>();
            color = iconSlots[i - 1].color;
            iconSlots[i - 1].color = new Color(color.r, color.g, color.b, 0);

            rectSlots[i - 1] = rect.GetChild(i).GetComponent<RectTransform>();
        }
    }
    public static void Show(bool isOn)
    {
        instance.gameObject.SetActive(isOn);
        if (!isOn)
            return;

        //instance.___ 여러 번 쓰기 싫어서 함수로 호출
        instance.InitSlotIcons();

        UnitMgr.Battle_SetRenderOrder(isTurn: true);
    }
    private void InitSlotIcons()
    {
        Unit actor = UnitMgr.Battle_GetUnit(GameMgr.NowOrder);
        int index = actor.Data.Index;
        byte[] combo = Player.MemberCombo[index];

        SkillData skill;
        center = CameraMgr.Battle_ScreenToLocalInRect(actor.Pos);

        for (int i = 0; i < combo.Length; ++i)
        {
            if (combo[i] == 0)
            {
                iconSlots[i].gameObject.SetActive(false);
                continue;
            }

            skill = DataMgr.SkillTBL[combo[i]];
            iconSlots[i].sprite = ResourceMgr.SPIcon[skill.RcsCode];
            iconSlots[i].gameObject.SetActive(true);

            //시계방향: [상][우][하][좌] * 275f
            rectSlots[i].localPosition = center;
            switch (i)
            {
                case 0: endPos[i] = center + Vector3.up    * 300f;    break;
                case 1: endPos[i] = center + Vector3.right * 250f;    break;
                case 2: endPos[i] = center + Vector3.down  * 300f;    break;
                case 3: endPos[i] = center + Vector3.left  * 250f;    break;
            }
        }
    }

    public bool UpdateUI(bool active, float lerpWeight)
    {
        Vector3 positionEnd;
        Color color;
        float alphaEnd = active ? 1 : 0;

        //dim alpha 
        color = dimBlack.color;
        dimBlack.color = new Color(color.r, color.g, color.b, Mathf.Lerp(color.a, alphaEnd * 0.75f, IDxVALUE.LERP * lerpWeight));

        //slot position, alpha
        for (int i = 0; i < rectSlots.Length; ++i)
        {
            positionEnd = active ? endPos[i] : center;
            rectSlots[i].localPosition = Vector3.Lerp(rectSlots[i].localPosition, positionEnd, IDxVALUE.LERP * lerpWeight);

            color = iconSlots[i].color;
            iconSlots[i].color = new Color(color.r, color.g, color.b, Mathf.Lerp(color.a, alphaEnd, IDxVALUE.LERP * lerpWeight));
        }


        if (!active && iconSlots[0].color.a <= 0.01f)
            return true;

        return false;
    }
}
