using Umpire.Vsa.Contracts.Events;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed record SessionEnded(string Session, string Folder, string Reason) : IntegrationEvent;
}
