using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Umpire.Vsa.Presenters;

namespace Umpire.Unity {
    // Binds the views of the loaded scenes to the presenters of the app scope. It injects each loaded scene at the
    // app start, and each scene that loads after that. Unity raises sceneLoaded after Awake and before Start, so a
    // view has its presenter in Start. A view that has a presenter already (Bind, VContainer) keeps it.
    // A view that Instantiate makes after the scene load gets no presenter: a factory must call Bind.
    // At the app stop, the views that it bound release their presenters, because the app scope disposes them.
    sealed class ViewInjector : IDisposable {
        readonly IServiceProvider presenters;
        readonly List<(IPresenterView View, Presenter Presenter)> bound = new();

        public ViewInjector(IServiceProvider presenters) {
            this.presenters = presenters;
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
            foreach (var (view, presenter) in bound) {
                if (view as UnityEngine.Object != null) {
                    view.Release(presenter);
                }
            }
            bound.Clear();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Inject(scene);

        // Also inactive views: they have their presenter when they become active.
        void Inject(Scene scene) {
            if (!scene.IsValid()) {
                return;
            }
            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var view in root.GetComponentsInChildren<IPresenterView>(true)) {
                    if (view.Presenter != null) {
                        continue;
                    }
                    try {
                        view.Bind(presenters);
                        bound.Add((view, view.Presenter));
                    } catch (Exception e) {
                        Debug.LogException(e, view as UnityEngine.Object);
                    }
                }
            }
        }
    }
}
