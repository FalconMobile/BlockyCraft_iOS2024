﻿using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay {


	public enum InputButtonNames {
		Button1,
		Button2,
		Jump,
		Up,
		Down,
		LeftControl,
		LeftShift,
		LeftAlt,
		Build,
		Fly,
		Crouch,
		Inventory,
		Light,
		ThrowItem,
		Action,
		MiddleButton,
		SeeThroughUp,
		SeeThroughDown,
        Escape,
        DebugWindow,
        Console,
		Thrust,
		Rotate,
		Custom1,
		Custom2,
		Custom3,
		Custom4,
		Custom5,
		Custom6,
		Custom7,
		Custom8,
		Custom9,
		Destroy
    }

	public enum InputButtonPressState {
		Idle,
		Down,
		Up,
		Pressed
	}

	public struct InputButtonState {
		public InputButtonPressState pressState;
		public float pressStartTime;
	}


	[DefaultExecutionOrder(-100)]
	public abstract class VoxelPlayInputController: MonoBehaviour {
		/// <summary>
		/// Horizontal input axis
		/// </summary>
		[NonSerialized]
		public float horizontalAxis;

		/// <summary>
		/// Vertical input axis
		/// </summary>
		[NonSerialized]
		public float verticalAxis;

		/// <summary>
		/// Vertical input axis
		/// </summary>
		[NonSerialized]
		public bool anyAxisButtonPressed;

		/// <summary>
		/// Horizontal mouse axis
		/// </summary>
		[NonSerialized]
		public float mouseX;

		/// <summary>
		/// Vertical mouse axis
		/// </summary>
		[NonSerialized]
		public float mouseY;

		/// <summary>
		/// Vertical mouse axis
		/// </summary>
		[NonSerialized]
		public float mouseScrollWheel;

		/// <summary>
		/// Location of cursor on screen
		/// </summary>
		[NonSerialized]
		public Vector3 screenPos;

		/// <summary>
		/// If cursor is inside screen
		/// </summary>
		[NonSerialized]
		public bool focused = true;

        /// <summary>
        /// Returns true if any button or key is pressed
        /// </summary>
        [NonSerialized]
        public bool anyKey;

        /// <summary>
        /// Maximum time in seconds between button press and release to inform a click
        /// </summary>
        public float clickTime = 0.2f;

		/// <summary>
		/// Disabled move/rotation controls but rest of input buttons are still processed like console, Esc, etc.
		/// </summary>
		public virtual new bool enabled { get; set; }

		[NonSerialized]
		public bool initialized;

		protected InputButtonState[] buttons;

		protected virtual bool Initialize () {
			return true;
		}

		protected abstract void UpdateInputState ();

		public bool GetButton (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Pressed;
		}

		public bool GetButtonDown (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Down;
		}

		public bool GetButtonUp (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Up;
		}

		public bool GetButtonClick (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Up && (Time.time - buttons [(int)button].pressStartTime) < clickTime;
		}

		bool ignoreThisFrame;

        public void Init () {
			int buttonCount = Enum.GetNames (typeof(InputButtonNames)).Length;
			buttons = new InputButtonState[buttonCount];
			initialized = Initialize ();
			enabled = true;
			ignoreThisFrame = true;
		}

        private void Update()
        {
	        if (!initialized)
	        {
		        return;
	        }

	        anyKey = Input.anyKey;
	        for (int k = 0; k < buttons.Length; k++)
	        {
		        if (buttons[k].pressState == InputButtonPressState.Up)
		        {
					buttons[k].pressState = InputButtonPressState.Idle;
		        }
	        }

	        if (!enabled)
	        {
		        mouseScrollWheel = mouseX = mouseY = horizontalAxis = verticalAxis = 0;
	        }

	        if (ignoreThisFrame)
	        {
		        ignoreThisFrame = false;
	        }
	        else
	        {
		        UpdateInputState();
	        }

	        if (!anyKey)
	        {
		        for (int k = 0; k < buttons.Length; k++)
		        {
			        if (buttons[k].pressState != InputButtonPressState.Idle)
			        {
				        anyKey = true;
				        break;
			        }
		        }
	        }
        }

		protected void ReadKeyState (InputButtonNames button, KeyCode keyCode) {
			if (Input.GetKeyDown (keyCode)) {
				buttons [(int)button].pressStartTime = Time.time;
				buttons [(int)button].pressState = InputButtonPressState.Down;
			} else if (Input.GetKeyUp (keyCode)) {
				buttons [(int)button].pressState = InputButtonPressState.Up;
			} else if (Input.GetKey (keyCode)) {
				buttons [(int)button].pressState = InputButtonPressState.Pressed;
			}
		}
	}
}
