namespace VSA.TestSuite.Spike
{
    internal sealed record FindObject(string Path) : IAction<FindResult>;
}
