using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The object found, the screen point of the touch, and the object that received the touch.
    /// Hit can differ from Name, for example if another object covers the point.
    /// </summary>
    public sealed record TouchObjectResult(string Path, string Name, Point Point, string Hit) : IResponse;
}
