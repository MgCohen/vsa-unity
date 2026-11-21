using System;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed class ScriptEnded : Exception
    {
        public ScriptEnded(string script)
            : base($"Script '{script}' ended the session")
        {
        }
    }
}
