using System.Text;
using System.Text.RegularExpressions;

namespace StatusLab
{
    /// <summary>log: size of the own unity.log, marker lines since the last sample, and the automation start line.</summary>
    public sealed class LogSource : ISignalSource
    {
        static readonly Regex Marker = new(
            @"STATUSLAB_|IsHumanControllingUs|error CS\d+|Reloading assemblies|Begin MonoManager ReloadAssembly|" +
            @"Exiting|Cleanup mono|Crash!!!|Asset Pipeline Refresh|Refresh completed|Starting compilation|Compilation|" +
            @"Safe Mode|scene backup|Bug ?Reporter|Cancelling DisplayDialog|EnterPlayMode|ExitPlayMode|playmode",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Automation = new(@"IsHumanControllingUs: (\d), StartBugReporterOnCrash: (\d)", RegexOptions.Compiled);

        string path;
        long offset;
        int? isHuman, startReporter;
        bool exitSeen, crashSeen;

        public string Key => "log";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (target.LogPath != path)
            {
                path = target.LogPath;
                offset = 0;
                isHuman = startReporter = null;
                exitSeen = crashSeen = false;
            }
            if (path == null || !File.Exists(path)) return Task.CompletedTask;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            into.LogSize = stream.Length;
            if (stream.Length > offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                var count = (int)Math.Min(stream.Length - offset, 4 * 1024 * 1024);
                var buffer = new byte[count];
                var read = stream.Read(buffer, 0, count);
                var text = Encoding.UTF8.GetString(buffer, 0, read);
                var cut = text.LastIndexOf('\n');
                if (cut >= 0)
                {
                    offset += Encoding.UTF8.GetByteCount(text.Substring(0, cut + 1));
                    foreach (var raw in text.Substring(0, cut).Split('\n'))
                    {
                        var line = raw.TrimEnd('\r');
                        var a = Automation.Match(line);
                        if (a.Success)
                        {
                            isHuman = int.Parse(a.Groups[1].Value);
                            startReporter = int.Parse(a.Groups[2].Value);
                        }
                        if (line.Contains("Crash!!!")) crashSeen = true;
                        if (line.StartsWith("Exiting", StringComparison.OrdinalIgnoreCase) || line.Contains("Cleanup mono")) exitSeen = true;
                        if (Marker.IsMatch(line) && (into.LogLines ??= new()).Count < 20)
                            into.LogLines.Add(line.Length > 200 ? line.Substring(0, 200) : line);
                    }
                }
            }
            into.LogIsHuman = isHuman;
            into.LogStartBugReporter = startReporter;
            into.LogExitSeen = exitSeen;
            into.LogCrashSeen = crashSeen;
            return Task.CompletedTask;
        }
    }
}
