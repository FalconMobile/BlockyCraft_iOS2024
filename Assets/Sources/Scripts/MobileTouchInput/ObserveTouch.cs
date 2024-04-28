using System;
using UnityEngine;

namespace MobileTouchInput
{
    internal sealed class ObserveTouch : TouchInput
    {
        public ObserveTouch()
        {
            sense = 0.1f;
        }

        protected override bool IsGoodTouch(Touch touch) => !IsLeftHalfOfScreen(touch);

        protected override Vector2 Move(Touch touch)
        {
            Vector2 offset = touch.position - deltaMove;
            deltaMove = touch.position;
            offset *= sense;
            return offset;
        }

        protected override Vector2 Stationary(Touch touch) => Move(touch);

        protected override Vector2 Finish(Touch touch)
        {
            touchId = ENDED;
            return Vector2.zero;
        }
    }
}