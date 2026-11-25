using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Evidence
{
    internal sealed class GetFolderHandler : RequestHandler<GetFolder, FolderResult>
    {
        readonly TestSession session;

        public GetFolderHandler(TestSession session) => this.session = session;

        public override Task<Result<FolderResult>> HandleAsync(IEnvelope<GetFolder> envelope)
            => Task.FromResult<Result<FolderResult>>(new FolderResult(session.Id, session.Folder));
    }
}
