namespace VSA.TestSuite.Spike
{
    /// <summary>The object that was found, and its screen point in pixels from the lower left.</summary>
    public sealed record FindResult(string Path, string Name, float X, float Y);
}
