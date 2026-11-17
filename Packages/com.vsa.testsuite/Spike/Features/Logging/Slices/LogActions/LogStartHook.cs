using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts;
using VSA.Handlers;

namespace VSA.TestSuite.Spike
{
    /// <summary>One console line when a message starts. The indent shows the depth of the call.</summary>
    internal sealed class LogStartHook : Before<IMessage>
    {
        public const string Tag = "[Spike]";
        public const string StartFrame = "spike.start_frame";

        public override async Task<IMessage> BeforeAsync(IEnvelope<IMessage> envelope)
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
