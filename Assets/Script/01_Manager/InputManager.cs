using UnityEngine;

public class InputManager
{
    public int Update()
    {
        int input = 0;

        //Button Down
        if (Input.GetButtonDown("DOWN"))    { input |= IDxInput.DOWN;   }
        if (Input.GetButtonDown("UP"))      { input |= IDxInput.UP;     }
        if (Input.GetButtonDown("LEFT"))    { input |= IDxInput.LEFT;   }
        if (Input.GetButtonDown("RIGHT"))   { input |= IDxInput.RIGHT;  }
        if (Input.GetButtonDown("ENTER"))   { input |= IDxInput.ENTER;  }
        if (Input.GetButtonDown("CANCEL"))  { input |= IDxInput.CANCEL; }
        if (Input.GetButtonDown("ESCAPE"))  { input |= IDxInput.ESCAPE; }
        if (Input.GetButtonDown("ACTION"))  { input |= IDxInput.ACTION; }

        //Button Hold
        if (Input.GetButton("DOWN"))        { input |= IDxInput.DOWN_HOLD;   }
        if (Input.GetButton("UP"))          { input |= IDxInput.UP_HOLD;     }
        if (Input.GetButton("LEFT"))        { input |= IDxInput.LEFT_HOLD;   }
        if (Input.GetButton("RIGHT"))       { input |= IDxInput.RIGHT_HOLD;  }
        if (Input.GetButton("ACTION"))      { input |= IDxInput.ACTION_HOLD; }

        return input;
    }
}
