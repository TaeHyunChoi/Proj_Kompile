namespace Script.GameSystem
{
    using System.Threading;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Script.Data;
    using static Script.Data.DataType;

    public class GameplayInputSystem : ISystem
    {
        private readonly InputAction moveInput;
        private readonly InputAction enterInput;
        private readonly InputAction actionInput;

        private CancellationTokenSource cts;
        private IDxInput inputFlag;

        public IDxInput InputFlag => inputFlag;
        public CancellationToken Token => cts.Token;

        public GameplayInputSystem()
        {
            inputFlag = IDxInput.NONE;

            #region initialize input
            moveInput = new InputAction("Move", InputActionType.Value);
            moveInput.AddCompositeBinding("2DVector")
                     .With("Up", "<Keyboard>/upArrow")
                     .With("Down", "<Keyboard>/downArrow")
                     .With("Left", "<Keyboard>/leftArrow")
                     .With("Right", "<Keyboard>/rightArrow");
            moveInput.performed += OnMovePerformed;
            moveInput.canceled += OnMoveCanceled;

            enterInput = new InputAction("Enter", InputActionType.Button);
            enterInput.AddBinding("<Keyboard>/z");
            enterInput.started += (context) =>
            {
                inputFlag |= IDxInput.ENTER;
            };
            enterInput.canceled += (context) =>
            {
                inputFlag &= ~IDxInput.ENTER;
            };

            actionInput = new InputAction("Action", InputActionType.Button);
            actionInput.AddBinding("<Keyboard>/space");
            actionInput.started += (context) =>
            {
                inputFlag |= IDxInput.ACTION;
            };
            actionInput.performed += (context) =>
            {
                inputFlag |= IDxInput.ACTION;
            };
            actionInput.canceled += (context) =>
            {
                inputFlag &= ~IDxInput.ACTION;
            };

            moveInput.Enable();
            enterInput.Enable();
            actionInput.Enable();
            #endregion

            #region initialize cancellation token source
            cts = new CancellationTokenSource();
            #endregion

            //
            //IngameUpdateManager.Register(this);
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 direction = context.ReadValue<Vector2>();

            inputFlag &= ~IDxInput.MOVE_ALL;
            if (direction.x > 0.1f) { inputFlag |= IDxInput.RIGHT; }
            if (direction.x < -0.1f) { inputFlag |= IDxInput.LEFT; }
            if (direction.y > 0.1f) { inputFlag |= IDxInput.UP; }
            if (direction.y < -0.1f) { inputFlag |= IDxInput.DOWN; }
        }
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            inputFlag &= ~IDxInput.MOVE_ALL;
        }

        public void Reset()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();

            inputFlag = IDxInput.NONE;
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