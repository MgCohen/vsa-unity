using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    /// <summary>The object found, and how long the wait took in frames and in seconds.</summary>
    public sealed record WaitResult(string Path, string Name, int Frames, float Seconds) : IResponse;
}
