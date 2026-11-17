using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts;
using VSA.Handlers;

namespace VSA.TestSuite.Spike
{
    /// <summary>The hook sample, first half: one console line when an action starts.</summary>
    internal sealed class LogStartStep : BeforeStep<IAction>
    {
        public const string Tag = "[Spike]";
        public const string StartFrame = "spike.start_frame";

        public override async Task<IAction> BeforeAsync(IEnvelope<IAction> envelope)
        {
            await Awaitable.MainThreadAsync();
            var e = (Envelope)envelope;
            e.Items[StartFrame] = Time.frameCount;
            Debug.Log($"{Tag} {Indent(e)}start {e.Message} depth={e.Depth} frame={Time.frameCount}");
            return envelope.Message;
        }

        internal static string Indent(Envelope e) => new string(' ', e.Depth * 2);
    }
}
