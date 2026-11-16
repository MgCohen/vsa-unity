using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Requests;
using VSA.Presenters;
using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The static entry that commands call. The first call starts the VSA app. End stops it,
    /// so the next call starts a new app.
    /// </summary>
    internal static class Actions
    {
        public static async Task<TResponse> Run<TResponse>(IRequest<TResponse> request)
            where TResponse : IResponse
        {
            if (!VsaHost.IsRunning) SpikeApp.Run(Array.Empty<string>());
            using var scope = VsaHost.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            var result = await bus.Send(request);
            return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.ToString());
        }

        /// <summary>Stops the app. Returns false if no app ran.</summary>
        public static bool End()
        {
            var running = VsaHost.IsRunning;
            SpikeApp.Stop();
            return running;
        }
    }
}
