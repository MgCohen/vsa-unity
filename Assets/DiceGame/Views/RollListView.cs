using Umpire.Unity;
using UnityEngine;

namespace DiceGame.App.Views {
    // The list of the rolls of this scene. Each roll adds one copy of the entry prefab under the content.
    public sealed class RollListView : View<RollListController> {
        [SerializeField]
        RollEntryView entryPrefab;

        [SerializeField]
        Transform content;

        public RollEntryView EntryPrefab {
            get => entryPrefab;
            set => entryPrefab = value;
        }

        public Transform Content {
            get => content;
            set => content = value;
        }

        protected override void OnBind() {
            Target.ShowFromNow();
            Target.Changed += OnChanged;
        }

        protected override void OnRelease() => Target.Changed -= OnChanged;

        // Changed can come on any thread. After the switch, the view can be destroyed.
        async void OnChanged() {
            await Awaitable.MainThreadAsync();
            if (this != null && Target != null) {
                Target.ShowNewRolls(entryPrefab, content);
            }
        }
    }
}
