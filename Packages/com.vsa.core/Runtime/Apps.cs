using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine.Scripting;
using VSA.Composition;
using VSA.Module;

namespace VSA.Unity {
    // The app class when the project has none.
    [Preserve]
    public sealed class DefaultVsaApp : VsaApp<DefaultVsaApp> {
        [Preserve]
        public DefaultVsaApp() {
        }
    }

    // Finds the app class (a class that inherits VsaApp<TApp>) and calls its static members.
    static class Apps {
        // The assemblies that can have an app class: each assembly that references VSA.Composition.
        // The scan (no name) skips a test assembly (it references NUnit): its app classes are for the tests.
        // A name selects from all the assemblies, so a test can select its own app class.
        internal static IReadOnlyList<Assembly> Assemblies(bool tests = false) => AppScan.AssembliesThatReference(typeof(VsaBuilder).Assembly)
            .Where(assembly => tests || assembly.GetReferencedAssemblies().All(reference => reference.Name != "nunit.framework"))
            .ToList();

        // Each app class of the project. The default app is not in the list.
        internal static IReadOnlyList<Type> All(IEnumerable<Assembly> assemblies) => assemblies
            .Distinct()
            .SelectMany(TypesOf)
            .Where(type => type != typeof(DefaultVsaApp) && IsApp(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        // The app of a VsaHost: the class with the given full name, else the one app class, else the default app.
        internal static Type Find(string name, IEnumerable<Assembly> assemblies) {
            var apps = All(assemblies);
            if (!string.IsNullOrEmpty(name)) {
                return apps.FirstOrDefault(type => type.FullName == name)
                    ?? throw new InvalidOperationException(
                        $"VsaHost: no app class {name}. The app classes: {Names(apps)}.");
            }
            switch (apps.Count) {
                case 0:
                    return typeof(DefaultVsaApp);
                case 1:
                    return apps[0];
                default:
                    throw new InvalidOperationException(
                        $"VsaHost: the project has {apps.Count} app classes: {Names(apps)}. Write one in the App field of VsaHost.");
            }
        }

        internal static int Run(Type app, string[] args) {
            // A direct call for the default app, so IL2CPP makes the shared code of VsaApp<TApp>.
            if (app == typeof(DefaultVsaApp)) {
                return DefaultVsaApp.Run(args);
            }
            return (int)Invoke(Static(app).GetMethod(nameof(DefaultVsaApp.Run)), new object[] { args });
        }

        internal static void Stop(Type app) {
            if (app == typeof(DefaultVsaApp)) {
                DefaultVsaApp.Stop();
                return;
            }
            Invoke(Static(app).GetMethod(nameof(DefaultVsaApp.Stop)), null);
        }

        internal static bool IsRunning(Type app) {
            if (app == typeof(DefaultVsaApp)) {
                return DefaultVsaApp.IsRunning;
            }
            return (bool)Invoke(Static(app).GetProperty(nameof(DefaultVsaApp.IsRunning)).GetMethod, null);
        }

        static bool IsApp(Type type) =>
            type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && AppBase(type) != null
            && type.GetConstructor(Type.EmptyTypes) != null;

        // VsaApp<TApp> of the app class: it has the static members.
        static Type Static(Type app) => AppBase(app) ?? throw new ArgumentException($"{app.FullName} does not inherit VsaApp<TApp>.");

        static Type AppBase(Type type) {
            for (var b = type.BaseType; b != null; b = b.BaseType) {
                if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(VsaApp<>)) {
                    return b;
                }
            }
            return null;
        }

        static object Invoke(MethodInfo method, object[] args) {
            try {
                return method.Invoke(null, args);
            } catch (TargetInvocationException e) when (e.InnerException != null) {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        static string Names(IEnumerable<Type> apps) {
            var names = string.Join(", ", apps.Select(type => type.FullName));
            return names.Length == 0 ? "none" : names;
        }

        static IEnumerable<Type> TypesOf(Assembly assembly) {
            try {
                return assembly.GetTypes();
            } catch (ReflectionTypeLoadException e) {
                return e.Types.Where(type => type != null);
            }
        }
    }
}
