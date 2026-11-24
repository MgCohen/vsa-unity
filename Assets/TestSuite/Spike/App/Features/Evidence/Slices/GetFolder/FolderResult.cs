using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Evidence
{
    public sealed record FolderResult(string Session, string Folder) : IResponse;
}
