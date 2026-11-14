using Greeter;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VSA.Unity;

namespace VContainerSample {
    public sealed class GreetingLabel : PresenterComponent<GreeterPresenter> {
        [SerializeField] Text label;

        [Inject]
        public void Construct(GreeterPresenter presenter) => Bind(presenter);

        protected override void Render() => label.text = $"{Presenter.Last} ({Presenter.Count})";
    }
}
