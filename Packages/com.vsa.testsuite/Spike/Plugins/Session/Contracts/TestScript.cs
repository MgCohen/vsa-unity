using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The base of a test script. A game puts its scripts in any assembly that references the spike.
    /// scripts.run finds a script by its Name and runs it once.
    /// </summary>
    public abstract class TestScript
    {
        /// <summary>The name for scripts.run. Use a short name with no spaces.</summary>
        public abstract string Name { get; }

        public abstract Task Run(ScriptContext script);
    }
}
