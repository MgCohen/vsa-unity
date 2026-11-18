using System;
using System.Collections.Generic;

namespace Umpire.Unity {
    // The services that a VContainer root scope gives to the app that it starts. UnityHost adds them as singletons
    // when it composes that app. The pack makes the host with new(), so the scope cannot give them to the host.
    // The slot has a value only while the scope starts the app, on the main thread.
    static class HostImports {
        static IReadOnlyList<(Type Type, object Instance)> pending;

        internal static bool IsTaken => pending == null;

        internal static void Give(IReadOnlyList<(Type Type, object Instance)> imports) => pending = imports;

        internal static IReadOnlyList<(Type Type, object Instance)> Take() {
            var imports = pending ?? Array.Empty<(Type, object)>();
            pending = null;
            return imports;
        }
    }
}
