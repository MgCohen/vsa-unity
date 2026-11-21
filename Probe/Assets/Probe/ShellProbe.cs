using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Probe.Posts;
using Umpire.Unity;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;
using UnityEngine;

namespace Probe {
    // Checks the shell and the Unity host in a real frame loop: in a player, or in Play Mode.
    // First it runs the compose checks of ProbeRunner, then the shell checks, then it quits.
    // The shell stops the app on quit, so the last line is "on-stop".
    public sealed class ShellProbe : MonoBehaviour {
        int mainThread;

        void Awake() => mainThread = Thread.CurrentThread.ManagedThreadId;

        async void Start() {
            var failed = ProbeRunner.RunAll();
            failed += await Check("shell-running", Running);
            failed += await Check("shell-no-hosting", NoHosting);
            failed += await Check("shell-request", Request);
            failed += await Check("shell-presenter-event", PresenterEvent);
            failed += await Check("shell-logger", Logger);
            failed += await Check("shell-stop", StopAndRun);
            ProbeRunner.Line("done", failed == 0 ? "ok" : "FAIL " + failed + " failed");
            Quit(failed);
        }

        Task<string> Running() {
            var shell = Shell.Owner;
            if (shell == null || !shell.IsRunning || Shell.Services == null) {
                throw new Exception("no running app: owner " + (shell != null) + ", services " + (Shell.Services != null));
            }
            return Task.FromResult(shell.GetType().Name + " runs, starts " + ProbeShell.Starts);
        }

        // The Kernel has no Generic Host, so no Microsoft.Extensions.Hosting assembly loads.
        Task<string> NoHosting() {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.GetName().Name.StartsWith("Microsoft.Extensions.Hosting", StringComparison.Ordinal)) {
                    throw new Exception(assembly.GetName().Name + " is loaded");
                }
            }
            return Task.FromResult("no Hosting assembly");
        }

        // A request from the main thread. The await comes back on the main thread.
        async Task<string> Request() {
            using (var scope = Shell.Services.CreateScope()) {
                var added = await scope.ServiceProvider.GetRequiredService<IRequestSender>().SendAsync(new AddPostRequest("shell"));
                var thread = Thread.CurrentThread.ManagedThreadId;
                return added.IsSuccess && thread == mainThread
                    ? "id " + added.Value.Id + " on the main thread"
                    : throw new Exception("success " + added.IsSuccess + ", thread " + thread + ", main " + mainThread);
            }
        }

        // A request from a thread pool thread. The presenter event must run on the main thread.
        async Task<string> PresenterEvent() {
            using (var scope = Shell.Services.CreateScope()) {
                // Only a presenter scope gets events (Umpire #216).
                scope.ServiceProvider.GetRequiredService<PresenterScope>();
                var presenter = scope.ServiceProvider.GetRequiredService<PostsPresenter>();
                var requests = scope.ServiceProvider.GetRequiredService<IRequestSender>();
                var sender = await Task.Run(async () => {
                    await requests.SendAsync(new AddPostRequest("pool"));
                    return Thread.CurrentThread.ManagedThreadId;
                });
                var done = await Task.WhenAny(presenter.EventThread, Task.Delay(5000));
                if (done != presenter.EventThread) {
                    throw new Exception("no event in 5 s");
                }
                var thread = await presenter.EventThread;
                return "sent on thread " + sender + ", event on thread " + thread + (thread == mainThread ? " (main)" : "");
            }
        }

        Task<string> Logger() {
            var text = "probe log " + Guid.NewGuid().ToString("N");
            var seen = false;
            void OnLog(string message, string stack, LogType type) => seen |= message.Contains(text);
            Application.logMessageReceived += OnLog;
            try {
                Shell.Services.GetRequiredService<ILogger<ShellProbe>>().LogWarning(text);
            } finally {
                Application.logMessageReceived -= OnLog;
            }
            return Task.FromResult(seen ? "the Unity console got the log" : throw new Exception("no Unity log"));
        }

        // Stop calls OnStop and clears the app. Run starts it again with a new host.
        Task<string> StopAndRun() {
            var shell = Shell.Owner;
            var stops = ProbeShell.Stops;
            shell.Stop();
            if (shell.IsRunning || Shell.Services != null || ProbeShell.Stops != stops + 1) {
                throw new Exception("after Stop: running " + shell.IsRunning + ", stops " + ProbeShell.Stops);
            }
            shell.Run();
            return Task.FromResult(shell.IsRunning && Shell.Services != null
                ? "stopped and started again, starts " + ProbeShell.Starts
                : throw new Exception("Run after Stop did not start the app"));
        }

        static async Task<int> Check(string name, Func<Task<string>> body) {
            try {
                ProbeRunner.Line(name, "ok " + await body());
                return 0;
            } catch (Exception e) {
                ProbeRunner.Line(name, "FAIL " + e.GetType().Name + ": " + e.Message);
                return 1;
            }
        }

        static void Quit(int failed) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(failed);
#endif
        }
    }
}
