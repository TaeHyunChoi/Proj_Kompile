using UnityEngine;
using static Index.IDxInput;

public class InputMgr : MonoBehaviour
{
    private IInputHandler inputGetter;          //update
    private IInputHandler fixedInputGetter;     //fixed update
    private int input;                          //ÀÔ·Â°ª

    public void Update()
    {
        #region Get Input
        //Button Down
        if (Input.GetButtonDown("DOWN"))    { input |= DOWN;   }
        if (Input.GetButtonDown("UP"))      { input |= UP;     }
        if (Input.GetButtonDown("LEFT"))    { input |= LEFT;   }
        if (Input.GetButtonDown("RIGHT"))   { input |= RIGHT;  }
        if (Input.GetButtonDown("ENTER"))   { input |= ENTER;  }
        if (Input.GetButtonDown("CANCEL"))  { input |= CANCEL; }
        if (Input.GetButtonDown("ESCAPE"))  { input |= ESCAPE; }
        if (Input.GetButtonDown("ACTION"))  { input |= ACTION; }

        //Button Hold
        if (Input.GetButton("DOWN"))        { input |= DOWN_HOLD;   }
        if (Input.GetButton("UP"))          { input |= UP_HOLD;     }
        if (Input.GetButton("LEFT"))        { input |= LEFT_HOLD;   }
        if (Input.GetButton("RIGHT"))       { input |= RIGHT_HOLD;  }
        if (Input.GetButton("ACTION"))      { input |= ACTION_HOLD; }
        #endregion

        if (0 != input
            && null != inputGetter)
        {
            inputGetter.Input(input);
            input = 0;
        }
    }
    private void FixedUpdate()
    {
        if (0 != input
            && null != fixedInputGetter)
        {
            fixedInputGetter.Input(input);
            input = 0;
        }
    }

    public void SetInputGetter(IInputHandler getter)
    {
        inputGetter = getter;
    }
    public void SetFixedInputGetter(IInputHandler fixedGetter)
    {
        fixedInputGetter = fixedGetter;
    }

    public void ReleaseInputGetter()
    {
        inputGetter = null;
    }
    public void ReleaseFixedInputGetter()
    {
        fixedInputGetter = null;
    }
}
