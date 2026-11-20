using System;
using System.IO;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class ActivityLog
    {
        static readonly object Gate = new object();

        public static string File => Path.Combine(TraceFiles.Root(), "activity.log").Replace('\\', '/');

        public static void Write(string what)
        {
            var line = $"{DateTime.UtcNow:O} {what}{Environment.NewLine}";
            lock (Gate)
            {
                Directory.CreateDirectory(TraceFiles.Root());
                System.IO.File.AppendAllText(File, line);
            }
        }
    }
}
