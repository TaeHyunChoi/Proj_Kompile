using UnityEngine;
using static Index.IDxInput;

public class InputMgr : MonoBehaviour
{
    private IGetInput inputGetter;
    private IGetInput fixedInputGetter;
    private int input;

    public void Update()
    {
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

    public void SetInputGetter(IGetInput getter)
    {
        inputGetter = getter;
    }
    public void SetFixedInputGetter(IGetInput fixedGetter)
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
