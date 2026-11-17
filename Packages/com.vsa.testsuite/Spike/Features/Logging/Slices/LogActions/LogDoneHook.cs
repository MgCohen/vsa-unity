using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts;
using VSA.Handlers;

namespace VSA.TestSuite.Spike
{
    /// <summary>One console line when a message ends, also on a failure. LogStartHook gives the start frame.</summary>
    internal sealed class LogDoneHook : After<IMessage>
    {
        public override async Task AfterAsync(IEnvelope<IMessage> envelope)
        {
            await Awaitable.MainThreadAsync();
            var e = (Envelope)envelope;
            var frames = e.Items.TryGetValue(LogStartHook.StartFrame, out var start) ? Time.frameCount - (int)start : -1;
            var name = e.Message.GetType().Name;
            var head = $"{LogStartHook.Tag} {LogStartHook.Indent(e)}";
            if (e.Exception != null)
                Debug.LogWarning($"{head}failed {name} state={e.State} frames={frames} error={e.Exception.Message}");
            else if (e.Error != null)
                Debug.LogWarning($"{head}failed {name} state={e.State} frames={frames} error={e.Error}");
            else
                Debug.Log($"{head}done {name} state={e.State} frames={frames} result={e.Result?.Value}");
        }
    }
}
