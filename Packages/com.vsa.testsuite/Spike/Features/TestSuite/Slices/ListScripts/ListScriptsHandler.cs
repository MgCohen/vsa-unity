using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.TestSuite
{
    internal sealed class ListScriptsHandler : RequestHandler<ListScripts, ScriptList>
    {
        readonly ScriptCatalog catalog;

        public ListScriptsHandler(ScriptCatalog catalog) => this.catalog = catalog;

        public override Task<Result<ScriptList>> HandleAsync(Envelope<ListScripts> envelope)
            => Task.FromResult<Result<ScriptList>>(new ScriptList(catalog.Names()));
    }
}
