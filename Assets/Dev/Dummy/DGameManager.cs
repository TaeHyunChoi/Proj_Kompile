using System;
using UnityEngine;


public class DGameManager : MonoBehaviour
{
    private GameState currentState;
    private static int input;

    private delegate void DeleInput(int input);
    private DeleInput DeleInputUpdate;

    private void Awake()
    {
        currentState = GameState.Title;
        DeleInputUpdate = UpdateTitle;
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
        if (Input.GetButton("DOWN"))   { input |= DIDxINPUT.DOWN_HOLD; }
        if (Input.GetButton("UP"))     { input |= DIDxINPUT.UP_HOLD; }
        if (Input.GetButton("LEFT"))   { input |= DIDxINPUT.LEFT_HOLD; }
        if (Input.GetButton("RIGHT"))  { input |= DIDxINPUT.RIGHT_HOLD; }
        if (Input.GetButton("ACTION")) { input |= DIDxINPUT.ACTION_HOLD; }
        
        //# 처리
        if (input != 0)
        {
            DeleInputUpdate(input);
        }
    }

    //여기서 매니저급들을 나눠서 넘겨야할 것 같은데...
    //클래스 간의 연결고리를 설계해야 함.
    //Q. currentState를 누가, 어디서 바꾸니? A. SceneManager 이런 친구들이겠군!

    private void UpdateTitle(int input)
    {
        Debug.Log(Convert.ToString(input, 2));
    }
    private void UpdateOption(int input)
    { 
    
    }
    private void UpdateBattle(int input)
    { 
        
    }
    private void UpdateFiled(int input)
    { 
        
    }
}
