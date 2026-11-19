using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed class LogStartStep : BeforeStep
    {
        public const string Tag = "[Spike]";
        public const string StartFrame = "spike.start_frame";

        readonly StepLog steps;

        public LogStartStep(StepLog steps) => this.steps = steps;

        public static string Indent(Envelope envelope) => new string(' ', envelope.Depth * 2);

        public override Task<object> BeforeAsync(Envelope envelope)
        {
            envelope.Items[StartFrame] = Time.frameCount;
            var head = $"{Tag} {Indent(envelope)}start step={steps.Next()} {envelope.Message}";
            Debug.Log($"{head} depth={envelope.Depth} frame={Time.frameCount}");
            return Task.FromResult(envelope.Message);
        }
    }
}
