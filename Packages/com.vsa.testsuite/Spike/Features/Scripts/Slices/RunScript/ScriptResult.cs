using VSA.Contracts.Requests;

namespace VSA.TestSuite.Spike
{
    /// <summary>The script that ran, its step count, its time in seconds, and the session id.</summary>
    public sealed record ScriptResult(string Script, int Steps, float Seconds, string Session) : IResponse;
}
