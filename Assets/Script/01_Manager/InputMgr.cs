using UnityEngine;
using static Index.IDxInput;

public class InputMgr
{
    public static bool TryGetInput(out int input)
    {
        input = 0;

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

        return 0 != input;
    }
}
