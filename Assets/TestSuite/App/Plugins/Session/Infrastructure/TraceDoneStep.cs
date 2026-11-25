using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed class TraceDoneStep : AfterStep
    {
        readonly TraceRecorder trace;
        readonly TestSession session;

        public TraceDoneStep(TraceRecorder trace, TestSession session)
        {
            this.trace = trace;
            this.session = session;
        }

        public override async Task AfterAsync(Envelope envelope)
        {
            var node = trace.Finish(envelope);
            if (node == null) return;
            var head = $"{TraceStartStep.Tag} {TraceStartStep.Indent(envelope)}";
            var tail = $"state={envelope.State} frames={node.Frames}";
            if (node.Status == TraceNode.Ok)
            {
                Debug.Log($"{head}done {node.Name} {tail} result={envelope.Result?.Value}");
                return;
            }
            Debug.LogWarning($"{head}failed {node.Name} {tail} error={node.Error}");
            if (!node.IsStep) return;
            var events = envelope.Services.GetRequiredService<IIntegrationEventPublisher>();
            var failed = new StepFailed(session.Id, node.Name, node.Folder, node.Error);
            await events.PublishAsync(failed, CancellationToken.None);
        }
    }
}
