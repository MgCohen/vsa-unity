using System;
using UnityEngine;

namespace VSA.Unity {
    // The entry of a VSA app in Unity: put it on one GameObject of the first scene.
    // Awake calls Run on the app class (VsaApp<TApp>): the class in the App field, else the one app class
    // of the project, else DefaultVsaApp. The app has the lifetime of the Unity app (DontDestroyOnLoad):
    // a scene change does not stop it, and a second VsaHost does nothing.
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("VSA/Vsa Host")]
    public sealed class VsaHost : MonoBehaviour {
        static VsaHost owner;

        [SerializeField, Tooltip("The full name of the app class. Empty: the one app class of the project, else DefaultVsaApp.")]
        string app = "";

        [SerializeField, Tooltip("The arguments of Run, for example --Modules:Counter:Max=5.")]
        string[] arguments = Array.Empty<string>();

        // The services of the app that runs now, else null.
        public static IServiceProvider Services => UnityVsaHost.Current?.Services;

        // True from Run until the app stops.
        public static bool IsRunning => UnityVsaHost.Current != null;

        // The VsaHost that owns the app, else null.
        public static VsaHost Owner => owner;

        public string App {
            get => app;
            set => app = value ?? "";
        }

        public string[] Arguments {
            get => arguments;
            set => arguments = value ?? Array.Empty<string>();
        }

        // The app class that Run uses. Set in Awake.
        public Type AppType { get; private set; }

        // Starts the app again after Stop. If the app runs, it does nothing.
        public void Run() {
            if (owner != this || Apps.IsRunning(AppType)) {
                return;
            }
            Apps.Run(AppType, arguments);
        }

        // Stops the app. Run can start it again.
        public void Stop() {
            if (owner == this) {
                Apps.Stop(AppType);
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
            AppType = Apps.Find(app, Apps.Assemblies(tests: !string.IsNullOrEmpty(app)));
            Apps.Run(AppType, arguments);
        }

        void OnDestroy() {
            if (owner != this) {
                return;
            }
            owner = null;
            if (AppType != null) {
                Apps.Stop(AppType);
            }
        }

        // Without a domain reload (Enter Play Mode Options), the static fields keep their values.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => owner = null;
    }
}
