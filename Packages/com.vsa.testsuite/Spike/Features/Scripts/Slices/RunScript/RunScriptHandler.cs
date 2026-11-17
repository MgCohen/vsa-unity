using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts;
using VSA.Handlers.Requests;
using VSA.Presenters;

namespace VSA.TestSuite.Spike
{
    /// <summary>Makes the script from the catalog and runs it. Its steps go on the bus as children of this request.</summary>
    internal sealed class RunScriptHandler : RequestHandler<RunScript, ScriptResult>
    {
        readonly ScriptCatalog catalog;
        readonly IBus bus;
        readonly TestSession session;

        public RunScriptHandler(ScriptCatalog catalog, IBus bus, TestSession session)
        {
            this.catalog = catalog;
            this.bus = bus;
            this.session = session;
        }

        public override async Task<Result<ScriptResult>> HandleAsync(Envelope<RunScript> envelope)
        {
            await Awaitable.MainThreadAsync();
            var script = catalog.Create(envelope.Message.Name);
            var context = new ScriptContext(bus, script.Name, envelope.Cancellation);
            var start = Time.realtimeSinceStartup;
            await script.Run(context);
            return new ScriptResult(script.Name, context.Steps, Time.realtimeSinceStartup - start, session.Id);
        }
    }
}
