using System;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class SessionProgress
    {
        public static Action<string> Target { get; set; }

        public static void Ping(string what) => Target?.Invoke("still working: " + what);
    }
}
