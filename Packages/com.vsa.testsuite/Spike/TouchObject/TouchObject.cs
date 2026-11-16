namespace VSA.TestSuite.Spike
{
    internal sealed record TouchObject(string Path) : IAction<TouchObjectResult>;
}
