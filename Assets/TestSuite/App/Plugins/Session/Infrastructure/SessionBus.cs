using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using Umpire.Unity;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Plugins.Session
{
    public static class SessionBus
    {
        static CancellationTokenSource run = new CancellationTokenSource();

        public static async Task<TraceReply> Send<TResponse>(TestRequest<TResponse> request)
            where TResponse : IResponse
        {
            var services = App();
            await StartIfNeeded(services);
            var settings = services.GetRequiredService<SessionSettings>();
            var (ok, error) = await Run(services, request, settings.InputCapSeconds);
            var trace = services.GetRequiredService<TraceRecorder>();
            var entry = trace.Last;
            var path = trace.LastFile;
            var endRequest = services.GetRequiredService<TestSession>().EndRequest;
            if (endRequest != null) await End(endRequest);
            if (!ok) throw new InvalidOperationException($"{error} · trace: {path}");
            return new TraceReply("ok", entry?.Name, entry?.Index ?? 0, entry?.Seconds ?? 0, path);
        }

        public static async Task<EndResult> End(string reason)
        {
            if (!Shell.AppIsRunning) return new EndResult(false, null);
            var services = Shell.Services;
            var folder = services.GetRequiredService<TestSession>().Folder;
            await CloseSession(services, reason);
            run.Cancel();
            run.Dispose();
            run = new CancellationTokenSource();
            Shell.Owner?.Stop();
            return new EndResult(true, folder);
        }

        public static async Task<SessionInfo> Info()
        {
            await Awaitable.MainThreadAsync();
            var session = Shell.AppIsRunning ? Shell.Services.GetRequiredService<TestSession>() : null;
            return new SessionInfo(
                Application.platform.ToString(),
                Application.isEditor,
                Application.isPlaying,
                Time.frameCount,
                Math.Round(Time.realtimeSinceStartupAsDouble, 3),
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                System.Diagnostics.Process.GetCurrentProcess().Id,
                session?.Folder != null ? session.Id : null);
        }

        public static void AppStopping()
        {
            if (Shell.Services == null) return;
            CloseSession(Shell.Services, "app stop").ContinueWith(
                t => Debug.LogException(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        static IServiceProvider App()
        {
            if (!Shell.AppIsRunning)
                (Shell.Owner ?? throw new InvalidOperationException(
                    "No Umpire app runs. Put a TestSuiteShell in the first scene.")).Run();
            return Shell.Services;
        }

        static async Task StartIfNeeded(IServiceProvider services)
        {
            var session = services.GetRequiredService<TestSession>();
            if (session.Folder != null) return;
            services.GetRequiredService<TraceRecorder>().Start();
            using var scope = services.CreateScope();
            var events = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            await events.PublishAsync(new SessionStarted(session.Id, session.Folder), CancellationToken.None);
        }

        static async Task<(bool Ok, string Error)> Run<TResponse>(
            IServiceProvider services, IRequest<TResponse> request, int capSeconds)
            where TResponse : IResponse
        {
            using var cap = CancellationTokenSource.CreateLinkedTokenSource(run.Token);
            cap.CancelAfter(TimeSpan.FromSeconds(capSeconds));
            using var scope = services.CreateScope();
            try
            {
                var requests = scope.ServiceProvider.GetRequiredService<IRequestSender>();
                var result = await requests.SendAsync(request, cap.Token);
                return (result.IsSuccess, result.IsSuccess ? null : result.Error.ToString());
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        static async Task CloseSession(IServiceProvider services, string reason)
        {
            var session = services.GetRequiredService<TestSession>();
            var trace = services.GetRequiredService<TraceRecorder>();
            if (session.Folder == null || trace.Closed) return;
            trace.EndOpen();
            using (var scope = services.CreateScope())
            {
                var events = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
                await events.PublishAsync(new SessionEnded(session.Id, session.Folder, reason), CancellationToken.None);
            }
            trace.End(reason);
        }
    }
}
