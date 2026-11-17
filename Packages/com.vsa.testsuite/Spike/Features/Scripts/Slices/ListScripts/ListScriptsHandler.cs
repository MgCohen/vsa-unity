using System.Threading.Tasks;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace VSA.TestSuite.Spike
{
    internal sealed class ListScriptsHandler : RequestHandler<ListScripts, ScriptList>
    {
        readonly ScriptCatalog catalog;

        public ListScriptsHandler(ScriptCatalog catalog) => this.catalog = catalog;

        public override Task<Result<ScriptList>> HandleAsync(Envelope<ListScripts> envelope)
            => Task.FromResult<Result<ScriptList>>(new ScriptList(catalog.Names()));
    }
}
