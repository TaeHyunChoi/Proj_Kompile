using UnityEngine;


public class DGameManager : MonoBehaviour
{
    private static int input;

    private delegate void DeleInput(int input);
    private DeleInput DeleInputUpdate;

    void Update()
    {
        //## 입력 : 입력은 이렇게만 처리한다는 마인드.
        input = 0;

        if (Input.GetButtonDown("DOWN")   || Input.GetButton("DOWN"))   { input |= 1 << DINDEX.DOWN;   }
        if (Input.GetButtonDown("UP")     || Input.GetButton("UP"))     { input |= 1 << DINDEX.UP;     }
        if (Input.GetButtonDown("LEFT")   || Input.GetButton("LEFT"))   { input |= 1 << DINDEX.LEFT;   }
        if (Input.GetButtonDown("RIGHT")  || Input.GetButton("RIGHT"))  { input |= 1 << DINDEX.RIGHT;  }

        if (Input.GetButtonDown("ENTER"))  { input |= 1 << DINDEX.ENTER;  }
        if (Input.GetButtonDown("CANCEL"))
        {
            //Enter가 1이면 뒤집어야 하는데 + 이거 비트 연산으로 안되나?
            input ^= 1 << DINDEX.ENTER; 
        } 
        if (Input.GetButtonDown("ESCAPE")) { input |= 1 << DINDEX.ESCAPE; }
        if (Input.GetButtonDown("ACTION") || Input.GetButton("ACTION")) { input |= 1 << DINDEX.ACTION; }

        //## 처리 : 각각의 레이어를 어떻게 처리할 것인가? 설계 필요;
        if (input != 0)
        {
            DeleInputUpdate(input);
        }
    }
    void SetGameLayer(GameLayer layer)
    {
        switch (layer)
        {
            case GameLayer.UI:      DeleInputUpdate = InputUI;           break;
            case GameLayer.Field:   DeleInputUpdate = InputField;        break;
            case GameLayer.Battle:  DeleInputUpdate = InputBattle;       break;
            default: Debug.LogError($"Wrong Game Layer Type : {layer}"); break;
        }
    }

    private void InputUI(int input)
    { 
        //아니면 직접 UIMgr.Update()로 빠지는 방법도 있을테고?
    }
    private void InputField(int input)
    { 
    
    }
    private void InputBattle(int input)
    { 
        
    }
}
