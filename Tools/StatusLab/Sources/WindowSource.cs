namespace StatusLab
{
    /// <summary>win: visible dialog windows (#32770) of the own PID, the main window title, and the hung flag.
    /// Unity's busy window is also #32770, with the buttons "Cancel" and "Skip Transcoding". It is a busy state, not a dialog.</summary>
    public sealed class WindowSource : ISignalSource
    {
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
                if (windowClass == "UnitySplashWindow")
                {
                    // The startup window: its bar is the progress of a project open.
                    foreach (var child in Win32.Children(hwnd))
                        if (Win32.ClassName(child).Contains("progress", StringComparison.OrdinalIgnoreCase) && Win32.Progress(child) is { } bar) into.SplashBar = bar.Pos;
                    continue;
                }
                if (windowClass != "#32770") continue;
                var title = Win32.Text(hwnd);
                var buttons = string.Join(",", Win32.Buttons(hwnd).Select(b => b.Label));
                if (IsBusyWindow(title, buttons))
                {
                    into.BusyWindow = title;
                    foreach (var child in Win32.Children(hwnd))
                    {
                        var cls = Win32.ClassName(child);
                        if (cls.Contains("progress", StringComparison.OrdinalIgnoreCase) && Win32.Progress(child) is { } bar) into.BusyBar = bar.Pos;
                        else if (cls == "Static" && Win32.Text(child) is { Length: > 0 } step) into.BusyStep = step;
                    }
                    var m = System.Text.RegularExpressions.Regex.Match(title, @"busy for (?:(\d+):)?(\d+)s?\)");
                    if (m.Success) into.BusySeconds = (m.Groups[1].Success ? int.Parse(m.Groups[1].Value) * 60 : 0) + int.Parse(m.Groups[2].Value);
                    continue;
                }
                into.DialogTitle ??= title;
                into.DialogButtons ??= buttons;
            }
            return Task.CompletedTask;
        }

        static readonly System.Text.RegularExpressions.Regex BusyTitle = new(
            @"^(Hold on|Reloading|Importing|Running managed|Compiling|Refresh|Busy|Loading|Building)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        /// <summary>Unity's busy window: the "Skip Transcoding" button, a known busy title, or an empty title.
        /// A sample can read the window before Unity has set its title and buttons.</summary>
        public static bool IsBusyWindow(string title, string buttons)
            => (buttons ?? "").Contains("Skip Transcoding") || string.IsNullOrWhiteSpace(title) || BusyTitle.IsMatch(title.Trim());
    }
}
