using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Factories;
using Umpire.Unity.Factories;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Umpire.Unity {
    // Binds the bound behaviours of the loaded scenes (View<T>, PresenterComponent<T>, Bound<T>) through the binder of
    // the app scope. A target comes from its factory, else from the app scope: one presenter or controller for each
    // app. It injects each loaded scene at the app start, and each scene that loads after that. Unity raises
    // sceneLoaded after Awake and before Start, so a behaviour has its target in Start. A behaviour that has a target
    // already (Bind, VContainer, a prefab factory) keeps it.
    // At the app stop, the behaviours that it bound release their targets, because the app scope disposes them.
    sealed class ViewInjector : IDisposable {
        readonly IBinder binder;
        readonly List<(IBoundBehaviour View, object Target)> bound = new();

        public ViewInjector(IServiceProvider services) {
            binder = services.GetRequiredService<IBinder>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (var i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) {
                    Inject(scene);
                }
            }
            if (Shell.Owner != null) {
                Inject(Shell.Owner.gameObject.scene);
            }
        }

        public void Dispose() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            foreach (var (view, target) in bound) {
                if (view as UnityEngine.Object != null) {
                    view.ReleaseTarget(target);
                }
            }
            bound.Clear();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Inject(scene);

        // Also inactive behaviours: they have their target when they become active.
        void Inject(Scene scene) {
            if (!scene.IsValid()) {
                return;
            }
            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var view in root.GetComponentsInChildren<IBoundBehaviour>(true)) {
                    if (view.BoundTarget != null) {
                        continue;
                    }
                    try {
                        binder.Bind(view);
                        bound.Add((view, view.BoundTarget));
                    } catch (Exception e) {
                        Debug.LogException(e, view as UnityEngine.Object);
                    }
                }
            }
        }
    }
}
