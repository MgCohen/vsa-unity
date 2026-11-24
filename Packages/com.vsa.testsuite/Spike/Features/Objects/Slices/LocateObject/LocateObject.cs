using Umpire.Vsa.Contracts.Commands;

namespace VSA.TestSuite.Spike.Features.Objects
{
    public sealed record LocateObject(string Path) : ICommand<ObjectPoint>;
}
