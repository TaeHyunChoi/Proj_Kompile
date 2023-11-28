using System.Collections;
using UnityEngine;


public class DGameManager : MonoBehaviour
{
    private IngameState currentState;
    private static int input;

    //private delegate void DeleInput(int input);
    //private DeleInput DeleInputUpdate;

    private static DGameManager instance;

    private void Awake()
    {
        currentState = IngameState.None;
        //this.enabled = false;
        instance = this;
    }

    private void Start()
    {
        //DDataLoader.ParseCSVToData<DSkillData>("DSkillData");
    }

    private void Update()
    {
        //# 입력
        input = 0;
        
        //Button Down
        if (Input.GetButtonDown("DOWN"))   { input |= DIDxINPUT.DOWN; }
        if (Input.GetButtonDown("UP"))     { input |= DIDxINPUT.UP; }
        if (Input.GetButtonDown("LEFT"))   { input |= DIDxINPUT.LEFT; }
        if (Input.GetButtonDown("RIGHT"))  { input |= DIDxINPUT.RIGHT; }
        if (Input.GetButtonDown("ENTER"))  { input |= DIDxINPUT.ENTER; }
        if (Input.GetButtonDown("CANCEL")) { input |= DIDxINPUT.CANCEL; }
        if (Input.GetButtonDown("ESCAPE")) { input |= DIDxINPUT.ESCAPE; }
        if (Input.GetButtonDown("ACTION")) { input |= DIDxINPUT.ACTION; }
        
        //Button Hold
        if (Input.GetButton("DOWN"))       { input |= DIDxINPUT.DOWN_HOLD; }
        if (Input.GetButton("UP"))         { input |= DIDxINPUT.UP_HOLD; }
        if (Input.GetButton("LEFT"))       { input |= DIDxINPUT.LEFT_HOLD; }
        if (Input.GetButton("RIGHT"))      { input |= DIDxINPUT.RIGHT_HOLD; }
        if (Input.GetButton("ACTION"))     { input |= DIDxINPUT.ACTION_HOLD; }
        
        //# 처리
        if (input != 0 && currentState != IngameState.None)
        {
            //DeleInputUpdate(input);
        }
    }
}
