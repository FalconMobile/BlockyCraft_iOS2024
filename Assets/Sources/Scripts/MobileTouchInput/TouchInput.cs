using System.Collections.Generic;
using UnityEngine;

namespace MobileTouchInput
{
    internal abstract class TouchInput
    {
        protected const int ENDED = -1;

        protected int touchId = ENDED;
        protected float sense = 1f;
        protected Vector2 deltaMove;

        private static readonly List<int> _touchesIdInUse = new List<int>();

        public Vector2 GetSimpleInput()
        {
            if (!TryGetTouch(out Touch touch))
            {
                if (!TrySetNewTouch(out touch))
                {
                    return Vector2.zero;
                }
            }

            return GetInputByTouchPhase(touch);
        }

        public Vector2 GetDetailedInput(out TouchPhase touchPhase, out Vector2 rawPosition)
        {
            if (!TryGetTouch(out Touch touch))
            {
                if (!TrySetNewTouch(out touch))
                {
                    touchPhase = TouchPhase.Ended;
                    rawPosition = Vector2.zero;
                    return Vector2.zero;
                }
            }

            touchPhase = touch.phase;
            rawPosition = touch.position;

            return GetInputByTouchPhase(touch);
        }

        private Vector2 GetInputByTouchPhase(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Moved:
                    return Move(touch);
                case TouchPhase.Stationary:
                    return Stationary(touch);
                case TouchPhase.Ended:
                    _touchesIdInUse.Remove(touch.fingerId);
                    return Finish(touch);
                default:
                    return Vector2.zero;
            }
        }

        protected abstract Vector2 Move(Touch touch);
        protected abstract Vector2 Stationary(Touch touch);
        protected abstract Vector2 Finish(Touch touch);
        protected abstract bool IsGoodTouch(Touch touch);

        protected bool IsLeftHalfOfScreen(Touch touch) => touch.position.x < Screen.width / 2f;
        protected bool IsBottomHalfOfScreen(Touch touch) => touch.position.y < Screen.height / 2f;

        private void SetTouch(Touch touch)
        {
            _touchesIdInUse.Remove(touchId);
            _touchesIdInUse.Add(touch.fingerId);

            touchId = touch.fingerId;
            deltaMove = touch.position;
        }

        private bool TrySetNewTouch(out Touch touch)
        {
            for (int k = 0; k < Input.touchCount; k++)
            {
                touch = Input.touches[k];
                if (_touchesIdInUse.Contains(touch.fingerId) || !IsGoodTouch(touch))
                {
                    continue;
                }

                SetTouch(touch);
                return true;
            }

            touch = default;
            return false;
        }

        private bool TryGetTouch(out Touch touch)
        {
            if (touchId == ENDED)
            {
                touch = default;
                return false;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.touches[i].fingerId == touchId)
                {
                    touch = Input.touches[i];
                    return true;
                }
            }

            touch = default;
            return false;
        }
    }
}