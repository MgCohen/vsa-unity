using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    /// <summary>Wait until an active object is at the path, for at most Seconds of real time.</summary>
    public sealed record WaitForObject(string Path, float Seconds) : IRequest<WaitResult>;
}
