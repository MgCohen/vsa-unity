using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Unity;
using Umpire.Vsa.Contracts.Requests;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class SessionBus
    {
        static CancellationTokenSource run = new CancellationTokenSource();

        public static async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
            where TResponse : IResponse
        {
            if (!Shell.AppIsRunning)
                (Shell.Owner ?? throw new InvalidOperationException(
                    "No Umpire app runs. Put a SpikeShell in the first scene.")).Run();
            using var scope = Shell.Services.CreateScope();
            var requests = scope.ServiceProvider.GetRequiredService<IRequestSender>();
            var result = await requests.SendAsync(request, run.Token);
            return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.ToString());
        }

        public static bool End()
        {
            var running = Shell.AppIsRunning;
            run.Cancel();
            run.Dispose();
            run = new CancellationTokenSource();
            if (running) Shell.Owner?.Stop();
            return running;
        }
    }
}
