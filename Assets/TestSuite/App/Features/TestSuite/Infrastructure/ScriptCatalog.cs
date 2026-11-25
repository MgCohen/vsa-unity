using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.TestSuite
{
    public sealed class ScriptCatalog
    {
        Dictionary<string, Type> scripts;

        public string[] Names() => Found().Keys.OrderBy(n => n).ToArray();

        public TestScript Create(string name)
            => Found().TryGetValue(name, out var type)
                ? (TestScript)Activator.CreateInstance(type)
                : throw new InvalidOperationException(
                    $"No test script '{name}'. Scripts: {string.Join(", ", Names())}");

        static Type[] Types(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
        }

        static bool IsScript(Type type)
            => !type.IsAbstract
                && typeof(TestScript).IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) != null;

        Dictionary<string, Type> Found()
        {
            if (scripts != null) return scripts;
            var suite = typeof(TestScript).Assembly;
            var suiteName = suite.GetName().Name;
            scripts = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies())
            {
                if (assembly != suite && assembly.GetReferencedAssemblies().All(r => r.Name != suiteName)) continue;
                foreach (var type in Types(assembly).Where(IsScript))
                    scripts[((TestScript)Activator.CreateInstance(type)).Name] = type;
            }
            return scripts;
        }
    }
}
