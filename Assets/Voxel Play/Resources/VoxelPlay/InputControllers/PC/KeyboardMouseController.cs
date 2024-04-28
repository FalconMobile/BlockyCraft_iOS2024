using UnityEngine;


namespace VoxelPlay
{

    public class KeyboardMouseController : VoxelPlayInputController
    {

        [Header ("Keymap")]
        public KeyCode keyJump = KeyCode.Space;
        public KeyCode keyUp = KeyCode.E;
        public KeyCode keyDown = KeyCode.Q;
        public KeyCode keyBuild = KeyCode.B;
        public KeyCode keyFly = KeyCode.F;
        public KeyCode keyCrouch = KeyCode.C;
        public KeyCode keyInventory = KeyCode.Tab;
        public KeyCode keyLight = KeyCode.L;
        public KeyCode keyThrowItem = KeyCode.G;
        public KeyCode keyAction = KeyCode.T;
        public KeyCode keySeeThroughUp = KeyCode.Q;
        public KeyCode keySeeThroughDown = KeyCode.E;
        public KeyCode keyEscape = KeyCode.Escape;
        public KeyCode keyConsole = KeyCode.F1;
        public KeyCode keyDebugWindow = KeyCode.F2;
        public KeyCode keyThrust = KeyCode.X;
        public KeyCode keyRotate = KeyCode.R;
        public KeyCode keyCustom1 = KeyCode.Alpha1;
        public KeyCode keyCustom2 = KeyCode.Alpha2;
        public KeyCode keyCustom3 = KeyCode.Alpha3;
        public KeyCode keyCustom4 = KeyCode.Alpha4;
        public KeyCode keyCustom5 = KeyCode.Alpha5;
        public KeyCode keyCustom6 = KeyCode.Alpha6;
        public KeyCode keyCustom7 = KeyCode.Alpha7;
        public KeyCode keyCustom8 = KeyCode.Alpha8;
        public KeyCode keyCustom9 = KeyCode.Alpha9;
        public KeyCode keyDestroy = KeyCode.N;


        protected override void UpdateInputState ()
        {
            screenPos = Input.mousePosition;

            mouseX = Input.GetAxis ("Mouse X");
            mouseY = Input.GetAxis ("Mouse Y");
            mouseScrollWheel = Input.GetAxis ("Mouse ScrollWheel");
            horizontalAxis = Input.GetAxis ("Horizontal");
            verticalAxis = Input.GetAxis ("Vertical");
            anyAxisButtonPressed = Input.GetAxisRaw ("Horizontal") != 0 || Input.GetAxisRaw ("Vertical") != 0;

            // Left mouse button
            if (Input.GetMouseButtonDown (0)) {
                buttons [(int)InputButtonNames.Button1].pressStartTime = Time.time;
                buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Down;
            } else if (Input.GetMouseButtonUp (0)) {
                buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Up;
            } else if (Input.GetMouseButton (0)) {
                buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Pressed;
            }
            // Right mouse button
            if (Input.GetMouseButtonDown (1)) {
                buttons [(int)InputButtonNames.Button2].pressStartTime = Time.time;
                buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Down;
            } else if (Input.GetMouseButtonUp (1)) {
                buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Up;
            } else if (Input.GetMouseButton (1)) {
                buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Pressed;
            }
            // Middle mouse button
            if (Input.GetMouseButtonDown (2)) {
                buttons [(int)InputButtonNames.MiddleButton].pressStartTime = Time.time;
                buttons [(int)InputButtonNames.MiddleButton].pressState = InputButtonPressState.Down;
            } else if (Input.GetMouseButtonUp (2)) {
                buttons [(int)InputButtonNames.MiddleButton].pressState = InputButtonPressState.Up;
            } else if (Input.GetMouseButton (2)) {
                buttons [(int)InputButtonNames.MiddleButton].pressState = InputButtonPressState.Pressed;
            }
            // Jump key
            ReadKeyState (InputButtonNames.Jump, keyJump);
            ReadKeyState (InputButtonNames.Up, keyUp);
            ReadKeyState (InputButtonNames.Down, keyDown);
            ReadKeyState (InputButtonNames.LeftControl, KeyCode.LeftControl);
            ReadKeyState (InputButtonNames.LeftShift, KeyCode.LeftShift);
            ReadKeyState (InputButtonNames.LeftAlt, KeyCode.LeftAlt);
            ReadKeyState (InputButtonNames.Build, keyBuild);
            ReadKeyState (InputButtonNames.Fly, keyFly);
            ReadKeyState (InputButtonNames.Crouch, keyCrouch);
            ReadKeyState (InputButtonNames.Inventory, keyInventory);
            ReadKeyState (InputButtonNames.Light, keyLight);
            ReadKeyState (InputButtonNames.ThrowItem, keyThrowItem);
            ReadKeyState (InputButtonNames.Action, keyAction);
            ReadKeyState (InputButtonNames.SeeThroughUp, keySeeThroughUp);
            ReadKeyState (InputButtonNames.SeeThroughDown, keySeeThroughDown);
            ReadKeyState (InputButtonNames.Escape, keyEscape);
            ReadKeyState (InputButtonNames.Console, keyConsole);
            ReadKeyState (InputButtonNames.DebugWindow, keyDebugWindow);
            ReadKeyState (InputButtonNames.Thrust, keyThrust);
            ReadKeyState (InputButtonNames.Rotate, keyRotate);
            ReadKeyState (InputButtonNames.Custom1, keyCustom1);
            ReadKeyState (InputButtonNames.Custom2, keyCustom2);
            ReadKeyState (InputButtonNames.Custom3, keyCustom3);
            ReadKeyState (InputButtonNames.Custom4, keyCustom4);
            ReadKeyState (InputButtonNames.Custom5, keyCustom5);
            ReadKeyState (InputButtonNames.Custom6, keyCustom6);
            ReadKeyState (InputButtonNames.Custom7, keyCustom7);
            ReadKeyState (InputButtonNames.Custom8, keyCustom8);
            ReadKeyState (InputButtonNames.Custom9, keyCustom9);
            ReadKeyState (InputButtonNames.Destroy, keyDestroy);
        }
    }
}
