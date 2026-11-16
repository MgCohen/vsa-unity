using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VSA.TestSuite
{
    /// <summary>The result of a click.</summary>
    public sealed class ClickResult
    {
        public float[] Point;
        public string Hit;
        public string Adapter;
        public int FrameBefore;
        public int FrameAfter;
    }

    /// <summary>Sends pointer input to the game. One implementation for each input path.</summary>
    public interface IPointerInput
    {
        string Name { get; }
        void Move(Vector2 p);
        void Press(Vector2 p, bool down);
    }

    /// <summary>Clicks at a screen point. Checks first that the expected object is the top UI hit.</summary>
    public sealed class PointerSystem
    {
        readonly IPointerInput input;

        public PointerSystem(IPointerInput input) => this.input = input;

        public async Task<ClickResult> Click(Vector2 p, GameObject expected, CancellationToken cancel)
        {
            if (!Application.isPlaying)
                throw new ActionFailed(FailCode.Failed, "A click needs Play Mode or a player");

            var hit = TopUiHit(p);
            if (expected != null && hit != null && hit != expected && !hit.transform.IsChildOf(expected.transform))
                throw new ActionFailed(FailCode.Blocked, "'" + hit.name + "' covers '" + expected.name + "'",
                    new { top = hit.name, expected = expected.name });

            int f0 = Time.frameCount;
            input.Move(p);
            await Frames.Wait(1, cancel);
            input.Press(p, true);
            await Frames.Wait(1, cancel);
            input.Press(p, false);
            await Frames.Wait(1, cancel);

            return new ClickResult
            {
                Point = new[] { p.x, p.y },
                Hit = hit == null ? null : hit.name,
                Adapter = input.Name,
                FrameBefore = f0,
                FrameAfter = Time.frameCount,
            };
        }

        static GameObject TopUiHit(Vector2 p)
        {
            var es = EventSystem.current;
            if (es == null) return null;
            var results = new List<RaycastResult>();
            es.RaycastAll(new PointerEventData(es) { position = p }, results);
            return results.Count > 0 ? results[0].gameObject : null;
        }
    }
}
