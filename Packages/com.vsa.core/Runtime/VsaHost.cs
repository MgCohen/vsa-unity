using System;
using UnityEngine;
using VSA.Composition;

namespace VSA.Unity {
    // The entry of a VSA app in Unity. The consumer closes it over its app class, in a file of the same name:
    //   public sealed class GameHost : VsaHost<MyApp> { }
    // Put the component on one GameObject of the first scene. Awake calls MyApp.Run. The app has the lifetime
    // of the Unity app (DontDestroyOnLoad): a scene change does not stop it, and a second VsaHost does nothing.
    public abstract class VsaHost<TApp> : VsaHost where TApp : VsaApp<TApp>, new() {
        public override Type AppType => typeof(TApp);

        protected override bool AppIsRunning => VsaApp<TApp>.IsRunning;

        protected override void RunApp(string[] args) => VsaApp<TApp>.Run(args);

        protected override void StopApp() => VsaApp<TApp>.Stop();
    }

    // The part of the host that does not know the app class: the running app for the views, and the Unity events.
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public abstract class VsaHost : MonoBehaviour {
        static VsaHost owner;

        [SerializeField, Tooltip("The arguments of Run, for example --Modules:Counter:Max=5.")]
        string[] arguments = Array.Empty<string>();

        // The services of the app that runs now, else null.
        public static IServiceProvider Services => UnityVsaHost.Current?.Services;

        // True from Run until the app stops.
        public static bool IsRunning => UnityVsaHost.Current != null;

        // The VsaHost that owns the app, else null.
        public static VsaHost Owner => owner;

        public string[] Arguments {
            get => arguments;
            set => arguments = value ?? Array.Empty<string>();
        }

        // The app class that the host runs.
        public abstract Type AppType { get; }

        protected abstract bool AppIsRunning { get; }

        protected abstract void RunApp(string[] args);

        protected abstract void StopApp();

        // Starts the app again after Stop. If the app runs, it does nothing.
        public void Run() {
            if (owner == this && !AppIsRunning) {
                RunApp(arguments);
            }
        }

        // Stops the app. Run can start it again.
        public void Stop() {
            if (owner == this) {
                StopApp();
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
            RunApp(arguments);
        }

        void OnDestroy() {
            if (owner != this) {
                return;
            }
            owner = null;
            StopApp();
        }

        // Without a domain reload (Enter Play Mode Options), the static fields keep their values.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => owner = null;
    }
}
