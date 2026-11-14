using Greeter;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace VContainerSample {
    public sealed class GreetButton : MonoBehaviour {
        [SerializeField] Button button;
        [SerializeField] string personName = "Unity";

        GreeterPresenter presenter;

        [Inject]
        public void Construct(GreeterPresenter presenter) => this.presenter = presenter;

        public void Greet() => _ = presenter?.GreetAsync(personName);

        void OnEnable() => button.onClick.AddListener(Greet);

        void OnDisable() => button.onClick.RemoveListener(Greet);
    }
}
