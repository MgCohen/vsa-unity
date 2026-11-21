using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VSA.TestSuite
{
    /// <summary>The result of a click or a hold.</summary>
    public sealed class ClickResult
    {
        public float[] Point;
        public string Hit;
        public string Adapter;
        public int FrameBefore;
        public int FrameAfter;
    }

    /// <summary>The result of a drag.</summary>
    public sealed class DragResult
    {
        public float[] From;
        public float[] To;
        public int Steps;
        public string Hit;
        public string Adapter;
        public int FrameBefore;
        public int FrameAfter;
    }

    /// <summary>Sends pointer input to the game. One implementation for each input path.</summary>
    public interface IPointerInput
    {
        string Name { get; }
        /// <summary>Moves the pointer. If the button is down, this is a drag.</summary>
        void Move(Vector2 p);
        void Press(Vector2 p, bool down);
    }

    /// <summary>
    /// Code that sees each pointer step after it goes to the game, for example a touch overlay.
    /// A hook must not change the input.
    /// </summary>
    public interface IPointerHook
    {
        void Moved(Vector2 p, bool down);
        void Pressed(Vector2 p);
        void Released(Vector2 p);
    }

    /// <summary>
    /// Clicks, holds and drags at screen points. Before it sends input, it checks that the
    /// expected object is the top UI hit. Each input step takes one frame, so the player loop
    /// processes it.
    /// </summary>
    public sealed class PointerSystem
    {
        readonly IPointerInput input;

        /// <summary>The pointer hooks, in call order. Add or remove hooks in the composition root.</summary>
        public List<IPointerHook> Hooks { get; } = new List<IPointerHook>();

        public PointerSystem(IPointerInput input) => this.input = input;

        void Move(Vector2 p, bool down)
        {
            input.Move(p);
            foreach (var h in Hooks) Safe(() => h.Moved(p, down));
        }

        void Press(Vector2 p, bool down)
        {
            input.Press(p, down);
            foreach (var h in Hooks) Safe(() => { if (down) h.Pressed(p); else h.Released(p); });
        }

        // A hook problem must not fail the input step.
        static void Safe(System.Action a)
        {
            try { a(); }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        public Task<ClickResult> Click(Vector2 p, GameObject expected, CancellationToken cancel)
            => Hold(p, 0, expected, cancel);

        /// <summary>Presses at a point, keeps the button down for <paramref name="holdMs"/>, then releases.</summary>
        public async Task<ClickResult> Hold(Vector2 p, int holdMs, GameObject expected, CancellationToken cancel)
        {
            var hit = CheckTarget(p, expected);
            int f0 = Time.frameCount;
            Move(p, false);
            await Frames.Wait(1, cancel);
            Press(p, true);
            await Frames.Wait(1, cancel);
            if (holdMs > 0) await Seconds(holdMs, cancel);
            Press(p, false);
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

        /// <summary>Presses at <paramref name="from"/>, moves in <paramref name="steps"/> frames to <paramref name="to"/>, then releases.</summary>
        public async Task<DragResult> Drag(Vector2 from, Vector2 to, int steps, GameObject expected, CancellationToken cancel)
        {
            steps = Mathf.Max(1, steps);
            var hit = CheckTarget(from, expected);
            int f0 = Time.frameCount;
            Move(from, false);
            await Frames.Wait(1, cancel);
            Press(from, true);
            await Frames.Wait(1, cancel);
            for (int i = 1; i <= steps; i++)
            {
                Move(Vector2.Lerp(from, to, i / (float)steps), true);
                await Frames.Wait(1, cancel);
            }
            Press(to, false);
            await Frames.Wait(1, cancel);

            return new DragResult
            {
                From = new[] { from.x, from.y },
                To = new[] { to.x, to.y },
                Steps = steps,
                Hit = hit == null ? null : hit.name,
                Adapter = input.Name,
                FrameBefore = f0,
                FrameAfter = Time.frameCount,
            };
        }

        static GameObject CheckTarget(Vector2 p, GameObject expected)
        {
            if (!Application.isPlaying)
                throw new ActionFailed(FailCode.Failed, "Pointer input needs Play Mode or a player");
            var hit = TopUiHit(p);
            if (expected != null && hit != null && hit != expected && !hit.transform.IsChildOf(expected.transform))
                throw new ActionFailed(FailCode.Blocked, "'" + hit.name + "' covers '" + expected.name + "'",
                    new { top = hit.name, expected = expected.name });
            return hit;
        }

        static async Task Seconds(int ms, CancellationToken cancel)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < ms)
            {
                cancel.ThrowIfCancellationRequested();
                await Task.Yield();
            }
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
