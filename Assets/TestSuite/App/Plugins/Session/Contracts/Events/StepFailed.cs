using Umpire.Vsa.Contracts.Events;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed record StepFailed(string Session, string Step, string Folder, string Error) : IntegrationEvent;
}
