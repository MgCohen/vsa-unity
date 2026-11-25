using System.Threading.Tasks;

namespace VSA.TestSuite.Plugins.Session
{
    public abstract class TestScript
    {
        public abstract string Name { get; }

        public abstract Task Run(ScriptContext script);
    }
}
