namespace Script.Manager
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Script.Index;
    using Script.Interface;
    using static Script.Index.IDxInput;
    using System.Collections.Generic;

    public class InputHandler : IIngameUpdater
    {
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;

        private static readonly List<IInputReceiver> inputReceivers = new List<IInputReceiver>();

        private static InputFlag inputFlag;
        public InputHandler()
        {
            inputFlag = InputFlag.NONE;

            moveInput = new InputAction("Move", InputActionType.Value);
            moveInput.AddCompositeBinding("2DVector")
                     .With("Up",    "<Keyboard>/upArrow")
                     .With("Down",  "<Keyboard>/downArrow")
                     .With("Left",  "<Keyboard>/leftArrow")
                     .With("Right", "<Keyboard>/rightArrow");
            moveInput.performed += OnMovePerformed;
            moveInput.canceled  += OnMoveCanceled;

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
        }
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();

            inputFlag &= ~InputFlag.MOVE_ALL;
            if (direction.x >  0.1f) { inputFlag |= InputFlag.RIGHT; }
            if (direction.x < -0.1f) { inputFlag |= InputFlag.LEFT;  }
            if (direction.y >  0.1f) { inputFlag |= InputFlag.UP;    }
            if (direction.y < -0.1f) { inputFlag |= InputFlag.DOWN;  }
        }
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            inputFlag &= ~InputFlag.MOVE_ALL;
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

        public static void AddInputReceiver(IInputReceiver receiver)
        {
            inputReceivers.Add(receiver);
        }
        public static void RemoveInputReceiver(IInputReceiver receiver)
        {
            inputReceivers.Remove(receiver);
        }

        public IngameUpdateState UpdateState()
        {
            if (InputFlag.NONE == inputFlag)
            {
                goto CLOSE;
            }

            for (int i = inputReceivers.Count - 1; i >= 0; --i)
            {
                Debug.Log($"[InputHandler] {System.Convert.ToString((int)inputFlag, 2)}");
                if (true == inputReceivers[i].ReceiveInput(inputFlag))
                {
                    goto CLOSE;
                }
            }

        CLOSE:
            return IngameUpdateState.RUNNING;
        }

        public static void Clear()
        {
            inputFlag = InputFlag.NONE;
        }
    }
}
