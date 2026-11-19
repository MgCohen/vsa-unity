using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using UnityEngine;
using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Scripts
{
    internal sealed class RunScriptHandler : RequestHandler<RunScript, ScriptResult>
    {
        readonly ScriptCatalog catalog;
        readonly IRequestSender requests;
        readonly TestSession session;

        public RunScriptHandler(ScriptCatalog catalog, IRequestSender requests, TestSession session)
        {
            this.catalog = catalog;
            this.requests = requests;
            this.session = session;
        }

        public override async Task<Result<ScriptResult>> HandleAsync(Envelope<RunScript> envelope)
        {
            var script = catalog.Create(envelope.Message.Name);
            var context = new ScriptContext(requests, script.Name, envelope.Cancellation);
            var start = Time.realtimeSinceStartup;
            await script.Run(context);
            return new ScriptResult(script.Name, context.Steps, Time.realtimeSinceStartup - start, session.Id);
        }
    }
}
