using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed class LogDoneStep : AfterStep
    {
        public override Task AfterAsync(Envelope envelope)
        {
            var frames = envelope.Items.TryGetValue(LogStartStep.StartFrame, out var start)
                ? Time.frameCount - (int)start
                : -1;
            var name = envelope.Message.GetType().Name;
            var head = $"{LogStartStep.Tag} {LogStartStep.Indent(envelope)}";
            var tail = $"state={envelope.State} frames={frames}";
            if (envelope.Exception != null)
                Debug.LogWarning($"{head}failed {name} {tail} error={envelope.Exception.Message}");
            else if (envelope.Error != null)
                Debug.LogWarning($"{head}failed {name} {tail} error={envelope.Error}");
            else
                Debug.Log($"{head}done {name} {tail} result={envelope.Result?.Value}");
            return Task.CompletedTask;
        }
    }
}
