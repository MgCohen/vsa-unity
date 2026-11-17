using VSA.Contracts;

namespace VSA.TestSuite.Spike
{
    /// <summary>The message base of the hooks pipeline. A request that is also an IAction goes through the hooks.</summary>
    public interface IAction : IMessage
    {
    }
}
