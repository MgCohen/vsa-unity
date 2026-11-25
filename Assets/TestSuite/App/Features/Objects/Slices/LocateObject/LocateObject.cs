using Umpire.Vsa.Contracts.Commands;

namespace VSA.TestSuite.Features.Objects
{
    public sealed record LocateObject(string Path) : ICommand<ObjectPoint>;
}
