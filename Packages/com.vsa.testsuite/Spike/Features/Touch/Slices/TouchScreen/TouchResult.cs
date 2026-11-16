namespace VSA.TestSuite.Spike
{
    // Plain floats: the CLI writes the result as JSON, and Vector2 does not serialize cleanly.
    public sealed record TouchResult(float X, float Y);
}
