using VSA.StatusChecker;
namespace StatusLab
{
    /// <summary>Records every top window of one process: class, title, visible flag, child controls, and the
    /// position of each progress control. Prints one line each time the picture changes.</summary>
    public static class WindowProbe
    {
        public static async Task Run(Func<int> pid, int seconds, Action<string> write, CancellationToken cancel)
        {
            string last = null;
            var until = DateTime.UtcNow.AddSeconds(seconds);
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow < until && !cancel.IsCancellationRequested)
            {
                var id = pid();
                if (id > 0)
                {
                    var parts = new List<string>();
                    foreach (var (hwnd, visible) in Win32.AllTopWindows(id))
                    {
                        var cls = Win32.ClassName(hwnd);
                        var title = Win32.Text(hwnd);
                        if (!visible && title.Length == 0) continue;   // hidden helper windows with no title
                        var children = Win32.Children(hwnd).Select(c =>
                        {
                            var ccls = Win32.ClassName(c);
                            var text = Win32.Text(c);
                            var bar = ccls.Contains("progress", StringComparison.OrdinalIgnoreCase) ? Win32.Progress(c) : null;
                            return ccls + (text.Length > 0 ? "'" + Trim(text) + "'" : "") + (bar is { } b ? "[" + b.Pos + "/" + b.Max + "]" : "");
                        });
                        parts.Add((visible ? "" : "(hidden) ") + cls + " '" + Trim(title) + "' {" + string.Join(", ", children) + "}");
                    }
                    var line = string.Join(" | ", parts);
                    if (line != last) write($"{(DateTime.UtcNow - start).TotalSeconds,6:0.0}s pid {id}: {line}");
                    last = line;
                }
                await Task.Delay(200, cancel);
            }
        }

        static string Trim(string text) => text.Length > 60 ? text.Substring(0, 60) : text;
    }
}
