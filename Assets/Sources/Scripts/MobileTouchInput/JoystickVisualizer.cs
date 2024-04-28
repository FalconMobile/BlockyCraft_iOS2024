using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileTouchInput
{
    public class JoystickVisualizer : MonoBehaviour
    {
        [SerializeField] private RectTransform joystick;
        [SerializeField] private RectTransform handler;
        [SerializeField] [Range(0, 150)] private float handlerShiftDistance;


        private readonly Dictionary<TouchPhase, Action<Vector2>> _actionsByTouchState =
            new Dictionary<TouchPhase, Action<Vector2>>();


        private readonly List<TouchPhase> _needRawInput = new List<TouchPhase>() {TouchPhase.Began, TouchPhase.Ended};


        public void Init()
        {
            _actionsByTouchState.Add(TouchPhase.Began, MoveToPosition);
            _actionsByTouchState.Add(TouchPhase.Moved, VisualizeHandler);
            _actionsByTouchState.Add(TouchPhase.Ended, SetInvisible);
        }

        private void MoveToPosition(Vector2 position)
        {
            joystick.transform.position = position;
            SetVisibility(true);
        }

        private void VisualizeHandler(Vector2 position)
        {
            bool isMoving = position.x != 0 || position.y != 0;

            if (!isMoving)
            {
                handler.transform.localPosition = Vector2.zero;
                return;
            }

            handler.transform.localPosition = position * handlerShiftDistance;
        }

        private void SetInvisible(Vector2 _) => SetInvisible();
        public void SetInvisible() => SetVisibility(false);

        private void SetVisibility(bool isVisible)
        {
            joystick.gameObject.SetActive(isVisible);
        }


        public void UpdateJoystick(TouchPhase touchPhase, Vector2 rawPosition, Vector2 moveInputData)
        {
            if (!_actionsByTouchState.ContainsKey(touchPhase))
            {
                return;
            }

            if (_needRawInput.Contains(touchPhase))
            {
                _actionsByTouchState[touchPhase]?.Invoke(rawPosition);
                return;
            }

            _actionsByTouchState[touchPhase]?.Invoke(moveInputData);
        }
    }
}