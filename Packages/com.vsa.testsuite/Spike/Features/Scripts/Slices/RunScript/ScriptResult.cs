using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Features.Scripts
{
    public sealed record ScriptResult(string Script, int Steps, float Seconds, string Session, bool EndedSession)
        : IResponse;
}
