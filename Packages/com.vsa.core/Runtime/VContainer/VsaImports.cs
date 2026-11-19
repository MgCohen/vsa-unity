using System;
using System.Collections.Generic;

namespace Umpire.Unity.VContainer {
    // The In direction: the VSA app gets services of the VContainer root scope.
    //   protected override void Import(VsaImports imports) => imports.Add<IAudio>();
    // VsaLifetimeScope resolves each type one time, after VContainer builds the root scope and before the app starts.
    // UnityHost gives the instance to the app as a singleton. VContainer owns it, so the app does not dispose it.
    public sealed class VsaImports {
        readonly List<Type> types = new List<Type>();

        public VsaImports Add<T>() where T : class => Add(typeof(T));

        public VsaImports Add(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (!types.Contains(type)) {
                types.Add(type);
            }
            return this;
        }

        internal IReadOnlyList<Type> Types => types;
    }
}
