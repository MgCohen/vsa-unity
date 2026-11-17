using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VSA.Composition;
using VSA.Contracts.Requests;
using VSA.Presenters;
using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The static way from a CLI command to the bus of the app. The Pipeline calls static methods only,
    /// so the commands cannot get the bus by injection. The first send finds the app with [CliApp]
    /// and starts it. End stops it, and the next send starts it again.
    /// </summary>
    public static class AppBus
    {
        static Type app;

        public static async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
            where TResponse : IResponse
        {
            if (!VsaHost.IsRunning) Call("Run", new object[] { Array.Empty<string>() });
            using var scope = VsaHost.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            var result = await bus.Send(request);
            return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.ToString());
        }

        /// <summary>Stops the app. Returns false if no app ran.</summary>
        public static bool End()
        {
            var running = VsaHost.IsRunning;
            if (running) Call("Stop", Array.Empty<object>());
            return running;
        }

        // VsaApp<TApp>.Run and Stop are static, so the call goes through the closed base type.
        static void Call(string method, object[] args)
            => (app ??= FindApp()).BaseType.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);

        static Type FindApp()
        {
            var apps = UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()
                .Where(a => a.GetReferencedAssemblies().Any(r => r.Name == "VSA.Composition"))
                .SelectMany(Types)
                .Where(t => t.IsDefined(typeof(CliAppAttribute), false))
                .ToList();
            if (apps.Count != 1)
                throw new InvalidOperationException($"The CLI needs one app with [CliApp]. Found {apps.Count}.");
            var type = apps[0];
            var isApp = type.BaseType is { IsGenericType: true } b && b.GetGenericTypeDefinition() == typeof(VsaApp<>);
            return isApp ? type : throw new InvalidOperationException($"{type.Name} has [CliApp] but is not a VsaApp.");
        }

        static Type[] Types(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
        }
    }
}
