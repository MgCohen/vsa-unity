using System;
using System.Collections.Generic;
using Umpire.Kernels;
using UnityEngine;

namespace Umpire.Unity {
    // The shell of an Umpire app over a pack: it makes the app and starts it. Derive from it to use the app hooks
    // or another pack, for example: public sealed class GameShell : Shell<Vsa<GameArchitecture, GameHost>> { }
    // A VSA app with the Unity host uses VsaShell (no hooks) or VsaShell<TArchitecture>.
    public abstract class Shell<TPack> : Shell, IAppEntry<TPack> where TPack : Pack, new() {
        protected override App CreateApp() => new ShellApp(this);

        protected virtual void OnStart() {
        }

        protected virtual void OnStop() {
        }

        // The app class of the shell. Each hook goes to the shell.
        sealed class ShellApp : App<TPack> {
            readonly Shell<TPack> shell;

            public ShellApp(Shell<TPack> shell) => this.shell = shell;

            protected override void OnStart() => shell.OnStart();

            protected override void OnStop() => shell.OnStop();
        }
    }

    // The part of the shell that does not know the pack. Put one shell on a GameObject of the first scene.
    // Awake starts the app. The app has the lifetime of the Unity app (DontDestroyOnLoad): a scene change does not
    // stop it, and a second shell does nothing. Unity destroys the shell at the exit of Play Mode and when the player
    // quits, and OnDestroy stops the app.
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public abstract class Shell : MonoBehaviour {
        static Shell owner;
        static App current;

        [SerializeField, Tooltip("The arguments of App.Start.")]
        string[] arguments = Array.Empty<string>();

        [SerializeField, Tooltip("The module settings. An asset wins over the UnitySettings classes in code.")]
        ModuleSettingsAsset[] settings = Array.Empty<ModuleSettingsAsset>();

        App app;

        // The shell that owns the app, else null.
        public static Shell Owner => owner;

        // The services of the app that runs now, else null. A shell or a VContainer root scope starts that app.
        public static IServiceProvider Services => current?.Services;

        // True while an app that a shell or a VContainer root scope started runs.
        public static bool AppIsRunning => current != null && current.State == AppState.Running;

        public string[] Arguments {
            get => arguments;
            set => arguments = value ?? Array.Empty<string>();
        }

        // The module settings assets of this shell.
        public IReadOnlyList<ModuleSettingsAsset> SettingsAssets => settings;

        // The state of the app of this shell: Stopped before the first start and after a stop.
        public AppState State => app != null ? app.State : AppState.Stopped;

        public bool IsRunning => State == AppState.Running;

        protected abstract App CreateApp();

        // Starts the app again after Stop. If the app runs or stops now, it does nothing.
        // The name is not Start, because Unity calls a method named Start as a message.
        public void Run() {
            if (owner == this && State == AppState.Stopped) {
                app.Start(arguments);
            }
        }

        // Stops the app. Run can start it again.
        public void Stop() {
            if (owner == this) {
                app?.Stop();
            }
        }

        // Makes the app the one that Services gives. Only one app can run at a time.
        internal static void Claim(App app, string by) {
            if (current != null) {
                throw new InvalidOperationException(
                    $"{by}: an Umpire app runs already. Use one shell or one VContainer root scope.");
            }
            current = app;
        }

        internal static void Release(App app) {
            if (current == app) {
                current = null;
            }
        }

        void Awake() {
            if (owner != null || current != null) {
                enabled = false;
                return;
            }
            owner = this;
            if (transform.parent != null) {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            app = CreateApp();
            Claim(app, name);
            app.Start(arguments);
        }

        void OnDestroy() {
            if (owner != this) {
                return;
            }
            owner = null;
            try {
                app?.Stop();
            } finally {
                Release(app);
            }
        }

        // Without a domain reload (Enter Play Mode Options), the static fields keep their values.
        // The name is not Reset, because Unity sends a Reset message when a component is added in the Editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStatics() {
            owner = null;
            current = null;
        }
    }
}
