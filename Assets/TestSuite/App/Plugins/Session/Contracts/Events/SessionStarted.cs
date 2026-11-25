using Umpire.Vsa.Contracts.Events;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed record SessionStarted(string Session, string Folder) : IntegrationEvent;
}
