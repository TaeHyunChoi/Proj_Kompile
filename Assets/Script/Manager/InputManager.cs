namespace Script.Manager
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using static Script.Index.IDxInput;

    /// <summary> 입력했니 or not 만 판단하여 flag로 저장 및 IngameManager에게 전달
    /// </summary>
    public class InputManager
    {
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;

        private static EInputFlag inputFlag;

        public InputManager()
        {
            moveInput = new InputAction("Move", InputActionType.Value);
            moveInput.AddCompositeBinding("2DVector")
                      .With("Up",    "<Keyboard>/upArrow")
                      .With("Down",  "<Keyboard>/downArrow")
                      .With("Left",  "<Keyboard>/leftArrow")
                      .With("Right", "<Keyboard>/rightArrow");
            //moveAction.AddBinding("<Gamepad>/leftStick");
            moveInput.started   += OnMove;
            moveInput.performed += OnMove;
            moveInput.canceled  += OnMove;

            enterInput = new InputAction("Enter", InputActionType.Button);
            enterInput.AddBinding("<Keyboard>/z");
            enterInput.started  += (context) => { inputFlag |=  EInputFlag.ENTER; };
            enterInput.canceled += (context) => { inputFlag &= ~EInputFlag.ENTER; };

            actionInput = new InputAction("Action", InputActionType.Button);
            actionInput.AddBinding("<Keyboard>/space");
            actionInput.started   += (context) => { inputFlag |=  EInputFlag.ACTION; };
            actionInput.canceled  += (context) => { inputFlag &= ~EInputFlag.ACTION; };
        }

        public static EInputFlag GetInputFlag()
        {
            return inputFlag;
        }
        public static void Clear()
        {
            inputFlag = EInputFlag.NONE;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            float x = direction.x;
            float y = direction.y;

            EInputFlag moveFlag = EInputFlag.NONE;
            if (x > 0) { moveFlag |= EInputFlag.RIGHT; }
            if (x < 0) { moveFlag |= EInputFlag.LEFT;  }
            if (y > 0) { moveFlag |= EInputFlag.UP;    }
            if (y < 0) { moveFlag |= EInputFlag.DOWN;  }

            inputFlag = (inputFlag & EInputFlag.ACT_ALL) | moveFlag;
        }

        public void OnEnable()
        {
            moveInput.Enable();
            enterInput.Enable();
            actionInput.Enable();
        }
        public void OnDisable()
        {
            moveInput.Disable();
            moveInput.Dispose();

            enterInput.Disable();
            enterInput.Dispose();

            actionInput.Disable();
            actionInput.Dispose();
        }
    }
}
