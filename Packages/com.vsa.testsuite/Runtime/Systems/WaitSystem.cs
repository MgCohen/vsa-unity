using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite
{
    /// <summary>The result of a wait.</summary>
    public sealed class WaitResult
    {
        public double Ms;
        public int Frames;
    }

    /// <summary>Waits for frames, for time, or until a condition is true. Checks once each frame.</summary>
    public sealed class WaitSystem
    {
        public async Task<WaitResult> Frames(int count, CancellationToken cancel)
        {
            var watch = Stopwatch.StartNew();
            int f0 = Time.frameCount;
            await VSA.TestSuite.Frames.Wait(count, cancel, Math.Max(5000, count * 100));
            return new WaitResult { Ms = Math.Round(watch.Elapsed.TotalMilliseconds, 1), Frames = Time.frameCount - f0 };
        }

        public async Task<WaitResult> Seconds(double seconds, CancellationToken cancel)
        {
            var watch = Stopwatch.StartNew();
            int f0 = Time.frameCount;
            while (watch.Elapsed.TotalSeconds < seconds)
            {
                cancel.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            return new WaitResult { Ms = Math.Round(watch.Elapsed.TotalMilliseconds, 1), Frames = Time.frameCount - f0 };
        }

        /// <summary>Returns when <paramref name="condition"/> is true. Fails with TimedOut after <paramref name="timeoutMs"/>.</summary>
        public async Task<WaitResult> Until(Func<bool> condition, int timeoutMs, string what, CancellationToken cancel)
        {
            var watch = Stopwatch.StartNew();
            int f0 = Time.frameCount;
            while (!condition())
            {
                cancel.ThrowIfCancellationRequested();
                if (watch.ElapsedMilliseconds > timeoutMs)
                    throw new ActionFailed(FailCode.TimedOut, "Waited " + timeoutMs + " ms for " + what);
                await Task.Yield();
            }
            return new WaitResult { Ms = Math.Round(watch.Elapsed.TotalMilliseconds, 1), Frames = Time.frameCount - f0 };
        }
    }
}
