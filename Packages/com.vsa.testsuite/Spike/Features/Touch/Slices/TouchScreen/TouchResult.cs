using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    /// <summary>The point touched, and the name of the object that received the touch (null for none).</summary>
    public sealed record TouchResult(Point Point, string Hit) : IResponse;
}
