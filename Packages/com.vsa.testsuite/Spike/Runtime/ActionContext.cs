using System.Threading;

namespace VSA.TestSuite.Spike
{
    internal sealed class ActionContext
    {
        public CancellationToken Cancel { get; }

        public ActionContext(CancellationToken cancel) => Cancel = cancel;
    }
}
