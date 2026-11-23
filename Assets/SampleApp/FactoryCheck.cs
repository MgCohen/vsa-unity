using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Factories;
using Umpire.Kernels;
using Umpire.Unity.Factories;
using UnityEngine;

namespace SampleApp {
    public sealed class Score {
        public int Value { get; set; } = 42;
    }

    // A plain C# product. The binder calls Bind through its MethodInfo, so the player must keep the method.
    public sealed class ScoreText : IBindable<Score> {
        public Score Target { get; private set; }

        public void Bind(Score target) => Target = target;
    }

    // A behaviour that a prefab factory binds.
    public sealed class ScoreBadge : Bound<Score> {
    }

    // The factory checks of the player: one plain product and one prefab copy.
    public static class FactoryCheck {
        public static IEnumerable<(string Name, string Value, string Expected)> Run() {
            var services = new ServiceCollection();
            services.AddFactory<Score>();
            services.AddFactory<ScoreText>();
            services.AddPrefabFactory<ScoreBadge>();
            using var provider = services.BuildServiceProvider();

            var text = provider.GetRequiredService<IFactory<ScoreText>>().Create();
            yield return ("factory-bind", text.Target?.Value.ToString() ?? "null", "42");

            var root = new GameObject("BadgePrefabs");
            root.SetActive(false);
            var prefab = new GameObject("Badge").AddComponent<ScoreBadge>();
            prefab.transform.SetParent(root.transform, false);
            var copy = provider.GetRequiredService<IPrefabFactory<ScoreBadge>>().Create(prefab);
            yield return ("prefab-bind", copy.Target?.Value.ToString() ?? "null", "42");
            Object.Destroy(copy.gameObject);
            Object.Destroy(root);
        }
    }
}
