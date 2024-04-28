using UnityEngine;

namespace MobileTouchInput
{
    internal sealed class MoveTouch : TouchInput
    {
        public MoveTouch()
        {
            sense = 0.6f;
        }

        protected override bool IsGoodTouch(Touch touch) => IsLeftHalfOfScreen(touch);

        protected override Vector2 Move(Touch touch)
        {
            Vector2 offset = touch.position - deltaMove;
            offset *= sense;
            return offset.normalized;
        }

        protected override Vector2 Stationary(Touch touch) => Move(touch);

        protected override Vector2 Finish(Touch touch)
        {
            touchId = ENDED;
            return Vector2.zero;
        }
    }
}