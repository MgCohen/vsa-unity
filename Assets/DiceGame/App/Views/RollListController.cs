using System;
using DiceGame.Features.Counter.Presenters;
using Umpire.Unity.Factories;
using UnityEngine;

namespace DiceGame.App.Views {
    // The controller of the roll list. It knows the counter, and it makes one RollEntryView for each new roll.
    public sealed class RollListController {
        readonly CounterPresenter counter;
        readonly IPrefabFactory<int, RollEntryView> entries;
        int shown = -1;

        public RollListController(CounterPresenter counter, IPrefabFactory<int, RollEntryView> entries) {
            this.counter = counter;
            this.entries = entries;
        }

        public event Action Changed {
            add => counter.Changed += value;
            remove => counter.Changed -= value;
        }

        // The rolls before this call are not in the list. A new scene starts an empty list.
        public void ShowFromNow() => shown = counter.Count;

        // Makes one entry for each roll after the last call.
        public void ShowNewRolls(RollEntryView prefab, Transform content) {
            while (shown >= 0 && shown < counter.Count) {
                shown++;
                entries.Create(prefab, shown, content);
            }
        }
    }
}
