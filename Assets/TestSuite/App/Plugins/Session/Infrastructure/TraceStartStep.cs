using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed class TraceStartStep : BeforeStep
    {
        public const string Tag = "[TestSuite]";

        readonly TraceRecorder trace;

        public TraceStartStep(TraceRecorder trace) => this.trace = trace;

        public static string Indent(Envelope envelope) => new string(' ', envelope.Depth * 2);

        public override Task<object> BeforeAsync(Envelope envelope)
        {
            var node = trace.Begin(envelope);
            var step = node.IsStep ? $"step={node.Index} " : "";
            var head = $"{Tag} {Indent(envelope)}start {step}{envelope.Message}";
            Debug.Log($"{head} depth={envelope.Depth} frame={node.StartFrame}");
            return Task.FromResult(envelope.Message);
        }
    }
}
