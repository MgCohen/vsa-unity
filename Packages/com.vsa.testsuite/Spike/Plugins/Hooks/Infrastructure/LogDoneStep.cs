using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts;
using VSA.Handlers;

namespace VSA.TestSuite.Spike
{
    /// <summary>The hook sample, second half: one console line when an action ends, also on a failure.</summary>
    internal sealed class LogDoneStep : AfterStep<IAction>
    {
        public override async Task AfterAsync(IEnvelope<IAction> envelope)
        {
            await Awaitable.MainThreadAsync();
            var e = (Envelope)envelope;
            var frames = e.Items.TryGetValue(LogStartStep.StartFrame, out var start) ? Time.frameCount - (int)start : -1;
            var name = e.Message.GetType().Name;
            var head = $"{LogStartStep.Tag} {LogStartStep.Indent(e)}";
            if (e.Exception != null)
                Debug.LogWarning($"{head}failed {name} state={e.State} frames={frames} error={e.Exception.Message}");
            else if (e.Error != null)
                Debug.LogWarning($"{head}failed {name} state={e.State} frames={frames} error={e.Error}");
            else
                Debug.Log($"{head}done {name} state={e.State} frames={frames} result={e.Result?.Value}");
        }
    }
}
