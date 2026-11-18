using System;
using System.Collections.Generic;
using Umpire.Kernels;
using UnityEngine;

namespace Umpire.Unity {
    // The shell of an Umpire app over a pack: it makes the app and starts it. Derive from it to use the app hooks
    // or another pack, for example: public sealed class GameShell : Shell<Vsa<GameArchitecture, UnityHost>> { }
    // With no hooks, put VsaShell on the GameObject.
    public abstract class Shell<TPack> : Shell where TPack : Pack, new() {
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

        [SerializeField, Tooltip("The arguments of App.Start.")]
        string[] arguments = Array.Empty<string>();

        [SerializeField, Tooltip("The module settings. An asset wins over the UnitySettings classes in code.")]
        ModuleSettingsAsset[] settings = Array.Empty<ModuleSettingsAsset>();

        App app;

        // The shell that owns the app, else null.
        public static Shell Owner => owner;

        // The services of the app that runs now, else null.
        public static IServiceProvider Services => owner != null && owner.app != null ? owner.app.Services : null;

        // The module settings assets of this shell.
        public IReadOnlyList<ModuleSettingsAsset> SettingsAssets => settings;

        public bool IsRunning => app != null && app.IsRunning;

        protected abstract App CreateApp();

        // Starts the app again after Stop. If the app runs, it does nothing.
        // The name is not Start, because Unity calls a method named Start as a message.
        public void Run() {
            if (owner == this && !IsRunning) {
                app.Start(arguments);
            }
        }

        // Stops the app. Run can start it again.
        public void Stop() {
            if (owner == this) {
                app?.Stop();
            }
        }

        void Awake() {
            if (owner != null) {
                enabled = false;
                return;
            }
            owner = this;
            if (transform.parent != null) {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            app = CreateApp();
            app.Start(arguments);
        }

        void OnDestroy() {
            if (owner != this) {
                return;
            }
            owner = null;
            app?.Stop();
        }

        // Without a domain reload (Enter Play Mode Options), the static fields keep their values.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => owner = null;
    }
}
