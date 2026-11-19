using System;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class SessionProgress
    {
        static int ticks;

        public static Action<string> Target { get; set; }

        public static void Tick(string what) => Target?.Invoke($"{++ticks}: {what}");

        public static void Ping(string what) => Tick("still working: " + what);
    }
}
