namespace VSA.TestSuite.Spike
{
    /// <summary>The object that was touched, and the screen point of the touch.</summary>
    public sealed record TouchObjectResult(string Path, string Name, float X, float Y);
}
