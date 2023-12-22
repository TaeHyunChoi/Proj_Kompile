using UnityEngine;

public class InputManager
{
    public delegate void InputDele(int input);
    private static InputDele inputFunc;
    private static int input;

    public void Update()
    {
        //# ют╥б
        input = 0;
        
        //Button Down
        if (Input.GetButtonDown("DOWN"))    { input |= IDx.DOWN; }
        if (Input.GetButtonDown("UP"))      { input |= IDx.UP; }
        if (Input.GetButtonDown("LEFT"))    { input |= IDx.LEFT; }
        if (Input.GetButtonDown("RIGHT"))   { input |= IDx.RIGHT; }
        if (Input.GetButtonDown("ENTER"))   { input |= IDx.ENTER; }
        if (Input.GetButtonDown("CANCEL"))  { input |= IDx.CANCEL; }
        if (Input.GetButtonDown("ESCAPE"))  { input |= IDx.ESCAPE; }
        if (Input.GetButtonDown("ACTION"))  { input |= IDx.ACTION; }

        //Button Hold
        if (Input.GetButton("DOWN"))        { input |= IDx.DOWN_HOLD; }
        if (Input.GetButton("UP"))          { input |= IDx.UP_HOLD; }
        if (Input.GetButton("LEFT"))        { input |= IDx.LEFT_HOLD; }
        if (Input.GetButton("RIGHT"))       { input |= IDx.RIGHT_HOLD; }
        if (Input.GetButton("ACTION"))      { input |= IDx.ACTION_HOLD; }

        if (input != 0)
        {
            inputFunc(input);
        }
    }

    public void Set(ContentType content)
    {
        switch (content)
        {
            case ContentType.Opening: inputFunc = OnOpening.Input;     break;
        }
    }
}
