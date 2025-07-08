namespace Script.Manager
{
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Script.Index;
    using Script.Interface;
    using static Script.Index.IDxInput;

    public class InputManager : IIngameUpdater
    {
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;

        private static InputFlag inputFlag;

        public InputManager()
        {
            inputFlag = InputFlag.NONE;

            moveInput = new InputAction("Move", InputActionType.Value, interactions: "Hold(duration=0.1)");
            moveInput.AddCompositeBinding("2DVector")
                     .With("Up",    "<Keyboard>/upArrow")
                     .With("Down",  "<Keyboard>/downArrow")
                     .With("Left",  "<Keyboard>/leftArrow")
                     .With("Right", "<Keyboard>/rightArrow");
            //moveAction.AddBinding("<Gamepad>/leftStick");

            // OnMove() : local function
            moveInput.started   += OnMove;
            moveInput.performed += OnMove;
            moveInput.canceled  += OnMove;

            enterInput = new InputAction("Enter", InputActionType.Button);
            enterInput.AddBinding("<Keyboard>/z");
            enterInput.started  += (context) => 
            { 
                inputFlag |=  InputFlag.ENTER;
            };
            enterInput.canceled += (context) => 
            { 
                inputFlag &= ~InputFlag.ENTER;
            };

            actionInput = new InputAction("Action", InputActionType.Button);
            actionInput.AddBinding("<Keyboard>/space");
            actionInput.started   += (context) => 
            { 
                inputFlag |=  InputFlag.ACTION;
            };
            actionInput.performed += (context) =>
            {
                inputFlag |= InputFlag.ACTION;
            };
            actionInput.canceled  += (context) => 
            { 
                inputFlag &= ~InputFlag.ACTION;
            };

            IngameUpdater.AddUpdater(this);

            static void OnMove(InputAction.CallbackContext context)
            {
                Vector2 direction = context.ReadValue<Vector2>();
                float x = direction.x;
                float y = direction.y;

                InputFlag moveFlag = InputFlag.NONE;
                if (x > 0) { moveFlag |= InputFlag.RIGHT; }
                if (x < 0) { moveFlag |= InputFlag.LEFT; }
                if (y > 0) { moveFlag |= InputFlag.UP; }
                if (y < 0) { moveFlag |= InputFlag.DOWN; }

                inputFlag = (inputFlag & InputFlag.ACT_ALL) | moveFlag;
            }
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

        public IngameUpdateState UpdateState()
        {
            try
            {
                if (InputFlag.NONE != inputFlag)
                {
                    MessageManager.Publish(new IngameMessage.OnInput(inputFlag));
                }
                return IngameUpdateState.RUNNING;
            }
            catch (Exception ex)
            {
                Debug.LogError($"InputManager.UpdateState 예외 발생: {ex}");
                return IngameUpdateState.FAILURE;
            }
        }
    }
}
