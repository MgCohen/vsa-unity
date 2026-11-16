using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>Move, press and release at one point, with one frame between each step.</summary>
    public sealed class TouchSystem
    {
        readonly ITouchDevice device;

        public TouchSystem(ITouchDevice device) => this.device = device;

        public async Task Touch(Vector2 point, CancellationToken cancel)
        {
            device.Move(point);
            await Awaitable.NextFrameAsync(cancel);
            device.Press(point, true);
            await Awaitable.NextFrameAsync(cancel);
            device.Press(point, false);
            await Awaitable.NextFrameAsync(cancel);
        }
    }
}
