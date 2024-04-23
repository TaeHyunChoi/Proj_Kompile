using UnityEngine;
using static Index.IDxInput;
using CMathf;

public class InputMgr : MonoBehaviour
{
    public IInputHandler      Updater { get; set; }
    public IFixedInputHandler FixedUpdater { get; set; }

    private EInput mInputNow;
    private EInput mInputPrev;

    private void Update()
    {
        mInputNow = EInput.NONE;

        //Button Down
        if (Input.GetButtonDown("DOWN"))    { mInputNow |= EInput.DOWN;   }
        if (Input.GetButtonDown("UP"))      { mInputNow |= EInput.UP;     }
        if (Input.GetButtonDown("LEFT"))    { mInputNow |= EInput.LEFT;   }
        if (Input.GetButtonDown("RIGHT"))   { mInputNow |= EInput.RIGHT;  }

        if (Input.GetButtonDown("ENTER"))   { mInputNow |= EInput.ENTER;  }
        if (Input.GetButtonDown("CANCEL"))  { mInputNow |= EInput.CANCEL; }
        if (Input.GetButtonDown("ESCAPE"))  { mInputNow |= EInput.ESCAPE; }
        if (Input.GetButtonDown("ACTION"))  { mInputNow |= EInput.ACTION; }

        //Button Hold
        if (Input.GetButton("DOWN"))        { mInputNow |= EInput.DOWN_HOLD;   }
        if (Input.GetButton("UP"))          { mInputNow |= EInput.UP_HOLD;     }
        if (Input.GetButton("LEFT"))        { mInputNow |= EInput.LEFT_HOLD;   }
        if (Input.GetButton("RIGHT"))       { mInputNow |= EInput.RIGHT_HOLD;  }
        if (Input.GetButton("ACTION"))      { mInputNow |= EInput.ACTION_HOLD; }

        //ADD: AXIS
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        if      (x > 0) { mInputNow |= (EInput.RIGHT | EInput.RIGHT_HOLD); }
        else if (x < 0) { mInputNow |= (EInput.LEFT  | EInput.LEFT_HOLD); }
        if      (z > 0) { mInputNow |= (EInput.UP    | EInput.UP_HOLD); }
        else if (z < 0) { mInputNow |= (EInput.DOWN  | EInput.DOWN_HOLD); }

        if (EInput.NONE != mInputNow || EInput.NONE != mInputPrev)
        {
            if (null != Updater)
            {
                Updater.Input(mInputNow);
                mInputPrev = mInputNow;
            }
        }
    }
    private void FixedUpdate()
    {
        if (EInput.NONE != mInputNow || EInput.NONE != mInputPrev)
        {
            if (null != FixedUpdater)
            {
                FixedUpdater.Input(mInputNow);
                mInputPrev = mInputNow;
            }
        }
    }

    public static Vector3 GetInputDirection(EInput input)
    {
        Vector3 dir = Vector3.zero;

        /*
        if (true == Compare(input, EInput.UP,    EInput.UP_HOLD))    { dir += Vector3.forward; }
        if (true == Compare(input, EInput.DOWN,  EInput.DOWN_HOLD))  { dir += Vector3.forward; }
        if (true == Compare(input, EInput.LEFT,  EInput.LEFT_HOLD))  { dir += Vector3.forward; }
        if (true == Compare(input, EInput.RIGHT, EInput.RIGHT_HOLD)) { dir += Vector3.forward; }
        */

        if (true == Compare(input, EInput.UP)    || true == Compare(input, EInput.UP_HOLD)) { dir += Vector3.forward; }
        if (true == Compare(input, EInput.DOWN)  || true == Compare(input, EInput.DOWN_HOLD)) { dir += Vector3.back; }
        if (true == Compare(input, EInput.LEFT)  || true == Compare(input, EInput.LEFT_HOLD)) { dir += Vector3.left; }
        if (true == Compare(input, EInput.RIGHT) || true == Compare(input, EInput.RIGHT_HOLD)) { dir += Vector3.right; }

        dir.Normalize();
        return CMath.FloorToVector(dir, 3);
    }
}
