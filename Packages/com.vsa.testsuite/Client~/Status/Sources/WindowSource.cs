using System.Text.RegularExpressions;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>win: visible dialog windows (#32770) of the own PID, the main window title, and the hung flag.
    /// Unity's busy window is also a #32770 window. The source finds it by its structure, not by its text:
    /// it has a progress control child (msctls_progress32). So the rule works in all Editor languages.</summary>
    public sealed class WindowSource : ISignalSource
    {
        public const string DialogClass = "#32770";
        public const string ProgressClass = "msctls_progress32";
        public const string SplashClass = "UnitySplashWindow";

        static readonly Regex BusyFor = new(@"\((?:[^()]*?)(?:(\d+):)?(\d+)s?\)", RegexOptions.Compiled);

        public string Key => "win";

        public Task Read(Snapshot into, Target target, CancellationToken cancel)
        {
            if (target.Pid <= 0 || target.Process == null || target.Process.HasExited) return Task.CompletedTask;
            var main = target.Process.MainWindowHandle;
            if (main != IntPtr.Zero)
            {
                into.MainTitle = Win32.Text(main);
                into.Hung = Win32.Hung(main);
            }
            foreach (var hwnd in Win32.TopWindows(target.Pid))
            {
                var windowClass = Win32.ClassName(hwnd);
                if (windowClass == SplashClass)
                {
                    // The startup window: its bar is the progress of a project open.
                    foreach (var child in Win32.Children(hwnd))
                        if (Win32.ClassName(child) == ProgressClass && Win32.Progress(child) is { } bar) into.SplashBar = bar.Pos;
                    continue;
                }
                if (windowClass != DialogClass) continue;
                var title = Win32.Text(hwnd);
                var children = Win32.Children(hwnd);
                if (IsBusyWindow(windowClass, children.Select(Win32.ClassName)))
                {
                    into.BusyWindow = title;
                    foreach (var child in children)
                    {
                        var cls = Win32.ClassName(child);
                        if (cls == ProgressClass && Win32.Progress(child) is { } bar) into.BusyBar = bar.Pos;
                        else if (cls == "Static" && Win32.Text(child) is { Length: > 0 } step) into.BusyStep = step;
                    }
                    into.BusySeconds = BusySeconds(title);
                    continue;
                }
                into.DialogTitle ??= title;
                into.DialogButtons ??= string.Join(",", Win32.Buttons(hwnd).Select(b => b.Label));
            }
            return Task.CompletedTask;
        }

        /// <summary>True for Unity's busy window: a #32770 window with a progress control child.
        /// A message box or another native dialog has no progress control.</summary>
        public static bool IsBusyWindow(string windowClass, IEnumerable<string> childClasses)
            => windowClass == DialogClass && childClasses.Contains(ProgressClass);

        /// <summary>The busy time in the title, for example "(busy for 31s)" or "(busy for 01:01)". Null if none.
        /// The number format does not depend on the Editor language; the words around it do.</summary>
        public static int? BusySeconds(string title)
        {
            var m = BusyFor.Match(title ?? "");
            if (!m.Success) return null;
            return (m.Groups[1].Success ? int.Parse(m.Groups[1].Value) * 60 : 0) + int.Parse(m.Groups[2].Value);
        }
    }
}
