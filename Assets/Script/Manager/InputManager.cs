namespace Script.Manager
{
    using System.Collections.Generic;
    using Script.Index;
    using Script.Interface;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using static Script.Index.IDxInput;

    public class InputManager
    {
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;

        private static List<(AssetCode index, IIngameInput input)> inputs;
        private static InputFlag inputFlag;


        public InputManager()
        {
            inputs    = new List<(AssetCode, IIngameInput)>();
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
            moveInput.canceled += OnMove;

            enterInput = new InputAction("Enter", InputActionType.Button);
            enterInput.AddBinding("<Keyboard>/z");
            enterInput.started  += (context) => 
            { 
                inputFlag |=  InputFlag.ENTER;
                Update();
            };
            enterInput.canceled += (context) => 
            { 
                inputFlag &= ~InputFlag.ENTER;
                Update();
            };

            actionInput = new InputAction("Action", InputActionType.Button);
            actionInput.AddBinding("<Keyboard>/space");
            actionInput.started   += (context) => 
            { 
                inputFlag |=  InputFlag.ACTION;
                Update();
            };
            actionInput.started += (context) =>
            {
                inputFlag |= InputFlag.ACTION;
                Update();
            };
            actionInput.canceled  += (context) => 
            { 
                inputFlag &= ~InputFlag.ACTION;
                Update();
            };

            void OnMove(InputAction.CallbackContext context)
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

                Update();
            }
        }


        public void Add(AssetCode targetAssetIndex, IIngameInput targetInput)
        {
            inputs.Add(new (targetAssetIndex, targetInput));
        }
        public void Update()
        {
            // 가장 최근에 추가된 것부터 먼저 입력 처리
            for (int i = inputs.Count - 1; i >= 0; --i)
            {
                inputs[i].input.Input(inputFlag);
            }
        }
        public void Remove(AssetCode assetIndex)
        {
            for (int i = inputs.Count - 1; i >= 0; --i)
            {
                if (inputs[i].index == assetIndex)
                {
                    inputs.RemoveAt(i);
                }
            }
        }


        public bool IsPerformed()
        {
            return moveInput.IsPressed() | actionInput.IsPressed();
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
