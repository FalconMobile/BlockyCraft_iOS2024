using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

namespace MobileTouchInput
{
    [RequireComponent(typeof(JoystickVisualizer))]
    public class MobileInput : VoxelPlayInputController
    {
        [SerializeField] private JoystickVisualizer joystickVisualizer;

        public static MobileInput Instance { get; private set; }
        private bool IsInventoryOpen => VoxelPlayUI.instance.IsInventoryVisible;

        private readonly MoveTouch _move = new MoveTouch();
        private readonly ObserveTouch _observe = new ObserveTouch();

        private readonly Queue<(int, InputButtonState)> _buttonEventsHolder = new Queue<(int, InputButtonState)>();


        private void Awake()
        {
            if (Instance != null)
            {
                return;
            }

            Instance = this;
        }

        protected override bool Initialize()
        {
            mouseScrollWheel = 0;
            screenPos = Vector3.zero;
            Input.simulateMouseWithTouches = false;

            joystickVisualizer.Init();

            return base.Initialize();
        }

        protected override void UpdateInputState()
        {
            HandleButtons();

            if (IsInventoryOpen)
            {
                joystickVisualizer.SetInvisible();
                return;
            }

            UpdateMoveData();
            UpdateObserveData();
        }

        private void HandleButtons()
        {
            while (_buttonEventsHolder.Count > 0)
            {
                var buttonEvent = _buttonEventsHolder.Dequeue();
                buttons[buttonEvent.Item1].pressStartTime = buttonEvent.Item2.pressStartTime;
                buttons[buttonEvent.Item1].pressState = buttonEvent.Item2.pressState;
            }
        }

        private void UpdateMoveData()
        {
            Vector2 moveInputData = _move.GetDetailedInput(out TouchPhase touchPhase, out Vector2 rawPosition);
            horizontalAxis = moveInputData.x;
            verticalAxis = moveInputData.y;

            anyAxisButtonPressed = horizontalAxis != 0 || verticalAxis != 0;

            joystickVisualizer.UpdateJoystick(touchPhase, rawPosition, moveInputData);
        }

        private void UpdateObserveData()
        {
            Vector2 observeInputData = _observe.GetSimpleInput();
            mouseX = observeInputData.x;
            mouseY = observeInputData.y;
        }


        public void OnButtonDown(InputButtonNames buttonName)
        {
            _buttonEventsHolder.Enqueue(((int)buttonName, new InputButtonState()
            {
                pressStartTime = Time.time,
                pressState = InputButtonPressState.Down
            }));
        }

        public void OnButtonPressed(InputButtonNames buttonName)
        {
            _buttonEventsHolder.Enqueue(((int)buttonName, new InputButtonState()
            {
                pressState = InputButtonPressState.Pressed
            }));
        }

        public void OnButtonUp(InputButtonNames buttonName, float pressedTime)
        {
            _buttonEventsHolder.Enqueue(((int)buttonName, new InputButtonState()
            {
                pressStartTime = pressedTime,
                pressState = InputButtonPressState.Up
            }));
        }

        public void OnButtonUp(InputButtonNames buttonName)
        {
            _buttonEventsHolder.Enqueue(((int)buttonName, new InputButtonState()
            {
                pressState = InputButtonPressState.Up
            }));
        }
    }
}