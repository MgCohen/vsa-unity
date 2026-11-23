using UnityEngine;
using UnityEngine.EventSystems;

namespace UISpike
{
    public sealed class ScriptedInput : BaseInput
    {
        Vector2 position;
        bool held;
        bool downNotYetRead;
        bool upNotYetRead;
        int downFrame = -1;
        int upFrame = -1;

        public override Vector2 mousePosition => position;
        public override bool mousePresent => true;
        public override bool touchSupported => false;
        public override int touchCount => 0;
        public override Vector2 mouseScrollDelta => Vector2.zero;

        public void MoveTo(Vector2 screenPosition) => position = screenPosition;

        public void Press()
        {
            held = true;
            downNotYetRead = true;
        }

        public void Release()
        {
            held = false;
            upNotYetRead = true;
        }

        public override bool GetMouseButton(int button) => button == 0 && held;

        public override bool GetMouseButtonDown(int button) =>
            button == 0 && IsInFirstReadFrame(ref downNotYetRead, ref downFrame);

        public override bool GetMouseButtonUp(int button) =>
            button == 0 && IsInFirstReadFrame(ref upNotYetRead, ref upFrame);

        static bool IsInFirstReadFrame(ref bool notYetRead, ref int firstReadFrame)
        {
            if (notYetRead)
            {
                notYetRead = false;
                firstReadFrame = Time.frameCount;
            }
            return firstReadFrame == Time.frameCount;
        }
    }
}
