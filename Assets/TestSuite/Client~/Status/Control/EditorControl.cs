using System.Diagnostics;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>The real control. It acts only on the own PID of the target.</summary>
    public sealed class EditorControl : IEditorControl
    {
        /// <summary>Runs the batch-mode compile check, then starts the Editor with -automated.
        /// Throws StartRefusedException if the check fails: a start would enter Safe Mode or exit.</summary>
        public Task Start(Target target, CancellationToken cancel) => EditorLauncher.StartChecked(target, cancel);

        public Task Quit(Target target, CancellationToken cancel) => QuitWith(target, "--discard", cancel);

        /// <summary>Sends "editor quit" with "--save", "--discard" or no flag (null). With no flag and unsaved changes,
        /// the Editor refuses. Returns the exit code of the unity CLI and its output.</summary>
        public static async Task<(int Exit, string Output)> QuitWith(Target target, string flag, CancellationToken cancel)
        {
            if (target.Pid <= 0) return (-1, null);
            var info = new ProcessStartInfo("unity")
            {
                WorkingDirectory = target.ProjectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var a in new[] { "command", "editor", "quit", flag, "--pid", target.Pid.ToString(), "--no-banner" })
                if (a != null) info.ArgumentList.Add(a);
            using var p = Process.Start(info);
            var output = p.StandardOutput.ReadToEndAsync();
            var error = p.StandardError.ReadToEndAsync();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeout.CancelAfter(30_000);
            try
            {
                await p.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
            {
                return (-1, "editor quit gave no answer in 30 s");
            }
            return (p.ExitCode, (await output + await error).Trim());
        }

        public void CloseWindow(Target target)
        {
            if (target.Process is { HasExited: false } p) p.CloseMainWindow();
        }

        public void Stop(Target target)
        {
            if (target.Process is { HasExited: false } p && p.Id == target.Pid) p.Kill();
        }

        public bool AnswerDialog(Target target, string titlePart, string button)
        {
            if (target.Pid <= 0) return false;
            foreach (var hwnd in Win32.TopWindows(target.Pid))
            {
                if (Win32.ClassName(hwnd) != "#32770") continue;
                if (!string.IsNullOrEmpty(titlePart) && !Win32.Text(hwnd).Contains(titlePart, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var (handle, label) in Win32.Buttons(hwnd))
                {
                    if (!label.Equals(button, StringComparison.OrdinalIgnoreCase)) continue;
                    Win32.Click(handle);
                    return true;
                }
            }
            return false;
        }
    }
}
