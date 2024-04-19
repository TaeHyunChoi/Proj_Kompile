using UnityEngine;
using static Index.IDxInput;

public class InputMgr : MonoBehaviour
{
    private IInputHandler      updater;         //update
    private IFixedInputHandler fixedUpdater;    //fixed update
    private int input;                          //ÀÔ·Â°ª

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
            && null != updater)
        {
            updater.Input(input);
            input = 0;
        }
    }
    private void FixedUpdate()
    {
        if (0 != input
            && null != fixedUpdater)
        {
            fixedUpdater.Input(input);
            input = 0;
        }
    }
    public void SetUpdater(IInputHandler getter)
    {
        updater = getter;
    }
    public void SetFixedUpdater(IFixedInputHandler fixedGetter)
    {
        fixedUpdater = fixedGetter;
    }

    public void ReleaseUpdater()
    {
        updater = null;
    }
    public void ReleaseFixedUpdater()
    {
        fixedUpdater = null;
    }
}
