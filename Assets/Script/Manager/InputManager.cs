namespace Script.Manager
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using static Script.Index.IDxInput;

    public class InputManager
    {
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;

        public InputManager()
        {
            moveInput = new InputAction("Move", InputActionType.Value);
            moveInput.AddCompositeBinding("2DVector")
                      .With("Up", "<Keyboard>/upArrow")
                      .With("Down", "<Keyboard>/downArrow")
                      .With("Left", "<Keyboard>/leftArrow")
                      .With("Right", "<Keyboard>/rightArrow");
            //moveAction.AddBinding("<Gamepad>/leftStick");

            moveInput.started   += OnMove;
            moveInput.performed += OnMove;
            moveInput.canceled  += OnStop;

            enterInput = new InputAction("Enter", InputActionType.Button);
            enterInput.AddBinding("<Keyboard>/z");
            enterInput.started   += OnEnterStarted;
            enterInput.performed += OnEnterPerformed;

            actionInput = new InputAction("Enter", InputActionType.Button);
            actionInput.AddBinding("<Keyboard>/space");
            actionInput.started   += OnActionStarted;
            actionInput.performed += OnActionPerformed;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            float x = direction.x;
            float y = direction.y;

            EInputFlag inputFlag = EInputFlag.NONE;
            if (x > 0) { inputFlag |= EInputFlag.RIGHT; }
            if (x < 0) { inputFlag |= EInputFlag.LEFT;  }
            if (y > 0) { inputFlag |= EInputFlag.UP;    }
            if (y < 0) { inputFlag |= EInputFlag.DOWN;  }

            IngameManager.SetInputValue(inputFlag);
        }
        private void OnStop(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            float x = direction.x;
            float y = direction.y;

            EInputFlag inputFlag = EInputFlag.ALL;
            if (x == 0) 
            {
                inputFlag &= ~(EInputFlag.LEFT | EInputFlag.LEFT_HOLD | EInputFlag.RIGHT | EInputFlag.RIGHT_HOLD);
            }
            if (y == 0)
            {
                inputFlag &= ~(EInputFlag.UP | EInputFlag.UP_HOLD | EInputFlag.DOWN | EInputFlag.DOWN_HOLD);
            }
            IngameManager.SetInputValue(inputFlag);
        }

        private void OnEnterStarted(InputAction.CallbackContext _)
        {
            IngameManager.SetInputValue(EInputFlag.ENTER);
        }
        private void OnEnterPerformed(InputAction.CallbackContext _)
        {
            IngameManager.SetInputValue(EInputFlag.ENTER_HOLD);
        }

        private void OnActionStarted(InputAction.CallbackContext _)
        {
            IngameManager.SetInputValue(EInputFlag.ACTION);
        }
        private void OnActionPerformed(InputAction.CallbackContext _)
        {
            IngameManager.SetInputValue(EInputFlag.ACTION_HOLD);
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
