using Umpire.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.App.Views {
    // One line of the roll list. A prefab factory makes it and binds it to its RollEntry.
    public sealed class RollEntryView : View<RollEntry> {
        [SerializeField]
        Text label;

        public Text Label {
            get => label;
            set => label = value;
        }

        protected override void OnBind() => label.text = "Roll " + Target.Number + ": " + Target.Value;
    }
}
