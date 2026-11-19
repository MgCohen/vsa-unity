using DiceGame.Features.Counter.Presenters;
using Umpire.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.App.Views {
    // The counter view: the roll button sends a roll, and the label shows the count and the last value.
    public sealed class CounterView : PresenterComponent<CounterPresenter> {
        [SerializeField]
        Button roll;

        [SerializeField]
        Text label;

        public Button RollButton {
            get => roll;
            set => roll = value;
        }

        public Text Label {
            get => label;
            set => label = value;
        }

        protected override void Render() {
            var error = Presenter.Roll.Error ?? Presenter.Load.Error;
            var last = Presenter.Count == 0 ? "-" : Presenter.Last.ToString();
            label.text = error != null ? "Error: " + error : "Rolls: " + Presenter.Count + "   Last: " + last;
        }

        void Start() {
            roll.onClick.AddListener(() => _ = Presenter?.RollAsync());
            _ = Presenter?.LoadAsync();
        }
    }
}
