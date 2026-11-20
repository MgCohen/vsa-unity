using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record StateResult(string Path, ObjectState State) : IResponse;
}
