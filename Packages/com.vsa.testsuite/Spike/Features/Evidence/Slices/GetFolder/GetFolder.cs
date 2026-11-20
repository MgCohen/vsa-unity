using Umpire.Vsa.Contracts.Requests;
using VSA.TestSuite.Spike.Plugins.Session;

namespace VSA.TestSuite.Spike.Features.Evidence
{
    public sealed record GetFolder : TestRequest, IRequest<FolderResult>;
}
