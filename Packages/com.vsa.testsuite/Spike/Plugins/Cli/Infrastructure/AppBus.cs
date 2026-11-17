using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Requests;
using VSA.Presenters;
using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The static way from a CLI command to the bus of the app. The Pipeline calls static methods only,
    /// so the commands cannot get the bus by injection. A VsaHost in the scene runs the app.
    /// After End, the next send asks that host to run the app again.
    /// </summary>
    public static class AppBus
    {
        public static async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
            where TResponse : IResponse
        {
            if (!VsaHost.IsRunning)
                (VsaHost.Owner ?? throw new InvalidOperationException(
                    "No VSA app runs. Put a VsaHost, for example SpikeHost, in the first scene.")).Run();
            using var scope = VsaHost.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            var result = await bus.Send(request);
            return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.ToString());
        }

        /// <summary>Stops the app of the host. Returns false if no app ran.</summary>
        public static bool End()
        {
            var running = VsaHost.IsRunning;
            if (running) VsaHost.Owner?.Stop();
            return running;
        }
    }
}
