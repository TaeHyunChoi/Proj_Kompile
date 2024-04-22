using UnityEngine;
using static Index.IDxInput;

public class InputMgr : MonoBehaviour
{
    private IInputHandler         updater;         //update
    private IFixedInputHandler    fixedUpdater;    //fixed update

    private EInput inputNow;
    private EInput inputPrev;

    public void Update()
    {
        inputNow = EInput.NONE;

        //Button Down
        if (Input.GetButtonDown("DOWN"))    { inputNow |= EInput.DOWN;   }
        if (Input.GetButtonDown("UP"))      { inputNow |= EInput.UP;     }
        if (Input.GetButtonDown("LEFT"))    { inputNow |= EInput.LEFT;   }
        if (Input.GetButtonDown("RIGHT"))   { inputNow |= EInput.RIGHT;  }

        if (Input.GetButtonDown("ENTER"))   { inputNow |= EInput.ENTER;  }
        if (Input.GetButtonDown("CANCEL"))  { inputNow |= EInput.CANCEL; }
        if (Input.GetButtonDown("ESCAPE"))  { inputNow |= EInput.ESCAPE; }
        if (Input.GetButtonDown("ACTION"))  { inputNow |= EInput.ACTION; }

        //Button Hold
        if (Input.GetButton("DOWN"))        { inputNow |= EInput.DOWN_HOLD;   }
        if (Input.GetButton("UP"))          { inputNow |= EInput.UP_HOLD;     }
        if (Input.GetButton("LEFT"))        { inputNow |= EInput.LEFT_HOLD;   }
        if (Input.GetButton("RIGHT"))       { inputNow |= EInput.RIGHT_HOLD;  }
        if (Input.GetButton("ACTION"))      { inputNow |= EInput.ACTION_HOLD; }

        //ADD: AXIS
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        if      (x > 0) { inputNow |= (EInput.RIGHT | EInput.RIGHT_HOLD); }
        else if (x < 0) { inputNow |= (EInput.LEFT  | EInput.LEFT_HOLD); }
        if      (z > 0) { inputNow |= (EInput.UP    | EInput.UP_HOLD); }
        else if (z < 0) { inputNow |= (EInput.DOWN  | EInput.DOWN_HOLD); }

        if (EInput.NONE != inputNow || EInput.NONE != inputPrev)
        {
            if (null != updater)
            {
                updater.Input(inputNow);
                inputPrev = inputNow;
            }
        }
    }
    private void FixedUpdate()
    {
        if (EInput.NONE != inputNow || EInput.NONE != inputPrev)
        {
            if (null != fixedUpdater)
            {
                fixedUpdater.Input(inputNow);
                inputPrev = inputNow;
            }
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
