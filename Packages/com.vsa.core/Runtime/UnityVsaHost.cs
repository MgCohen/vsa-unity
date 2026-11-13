using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityEngine;
using UnityEngine.Scripting;
using VSA.Composition;
using VSA.Module;
using VSA.Presenters;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VSA.Unity {
    // The Unity IVsaHost. The app selects it with UseUnityHost, else the host scan of VsaApp finds it.
    // Run starts the app and returns at once: Unity owns the process and the frame loop.
    // The app stops on Application.quitting, at the exit of the play mode, before a domain reload, or on VsaApp.Stop().
    [Preserve]
    sealed class UnityVsaHost : IVsaHost {
        static IHost current;

        // The app that runs now, or null.
        internal static IHost Current => current;

        public IHostApplicationBuilder CreateBuilder(string[] args) {
            // No defaults: the default .NET host fails in Unity (EventLog logger on Mono, content root Unity.exe\).
            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings {
                Args = args,
                ApplicationName = Application.productName,
                EnvironmentName = Application.isEditor ? Environments.Development : Environments.Production,
                ContentRootPath = Directory.Exists(Application.dataPath) ? Application.dataPath : Application.persistentDataPath,
            });
            builder.Configuration.AddCommandLine(args);
            builder.Logging.AddProvider(new UnityLogProvider());
            builder.Services.AddSingleton<IHostLifetime, UnityLifetime>();
            return builder;
        }

        // Presenter events run on the main thread, so a view can change Unity objects in Render.
        public void Install(IHostApplicationBuilder builder, ModuleTypes types) =>
            builder.Services.AddSingleton<LivePresenters.Dispatch>(OnMainThread);

        public IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();

        public int Run(IHost app) {
            app.StartAsync().GetAwaiter().GetResult();
            current = app;
            Application.quitting += StopCurrent;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += StopCurrent;
#endif
            return 0;
        }

        public void Stop(IHost app) {
            Application.quitting -= StopCurrent;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= StopCurrent;
#endif
            try {
                app.StopAsync().GetAwaiter().GetResult();
            } finally {
                app.Dispose();
                if (current == app) {
                    current = null;
                }
            }
        }

#if UNITY_EDITOR
        internal static void OnPlayMode(PlayModeStateChange change) {
            if (change == PlayModeStateChange.ExitingPlayMode) {
                StopCurrent();
            }
        }
#endif

        static void StopCurrent() {
            var app = current;
            if (app != null) {
                new UnityVsaHost().Stop(app);
            }
        }

        static async Task OnMainThread(Func<Task> work) {
            await Awaitable.MainThreadAsync();
            await work();
        }

        // Unity starts and stops the process, so the host does not wait for a signal.
        sealed class UnityLifetime : IHostLifetime {
            public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }

    public static class UnityHostExtensions {
        // Selects the Unity host in the Builder step of the app:
        //   protected override VsaBuilder Builder(string[] args) => base.Builder(args).UseUnityHost();
        public static VsaBuilder UseUnityHost(this VsaBuilder builder) => builder.UseHost(new UnityVsaHost());
    }
}
