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
    /// so the commands cannot get the bus by injection. The first send starts the app; End stops it.
    /// </summary>
    public static class AppBus
    {
        static Action start;
        static Action stop;

        /// <summary>The app calls this one time, so the plugin can start and stop it.</summary>
        public static void Use(Action startApp, Action stopApp)
        {
            start = startApp;
            stop = stopApp;
        }

        public static async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
            where TResponse : IResponse
        {
            if (!VsaHost.IsRunning)
                (start ?? throw new InvalidOperationException("No app uses the CLI plugin. Enter Play Mode first."))();
            using var scope = VsaHost.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            var result = await bus.Send(request);
            return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.ToString());
        }

        /// <summary>Stops the app. Returns false if no app ran.</summary>
        public static bool End()
        {
            var running = VsaHost.IsRunning;
            if (running) stop?.Invoke();
            return running;
        }
    }
}
