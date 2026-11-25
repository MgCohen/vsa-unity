using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Features.TestSuite
{
    internal sealed class ListScriptsHandler : RequestHandler<ListScripts, ScriptList>
    {
        readonly ScriptCatalog catalog;

        public ListScriptsHandler(ScriptCatalog catalog) => this.catalog = catalog;

        public override Task<Result<ScriptList>> HandleAsync(IEnvelope<ListScripts> envelope)
            => Task.FromResult<Result<ScriptList>>(new ScriptList(catalog.Names()));
    }
}
