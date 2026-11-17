using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Finds each TestScript class with a constructor that takes no arguments, in the loaded
    /// assemblies that reference the spike. The first use scans; later uses read the cache.
    /// </summary>
    public sealed class ScriptCatalog
    {
        Dictionary<string, Type> scripts;

        public string[] Names() => Found().Keys.OrderBy(n => n).ToArray();

        public TestScript Create(string name)
            => Found().TryGetValue(name, out var type)
                ? (TestScript)Activator.CreateInstance(type)
                : throw new InvalidOperationException($"No test script '{name}'. Scripts: {string.Join(", ", Names())}");

        Dictionary<string, Type> Found()
        {
            if (scripts != null) return scripts;
            var spike = typeof(TestScript).Assembly;
            scripts = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies())
            {
                if (assembly != spike && assembly.GetReferencedAssemblies().All(r => r.Name != spike.GetName().Name)) continue;
                foreach (var type in Types(assembly))
                {
                    if (type.IsAbstract || !typeof(TestScript).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null) continue;
                    scripts[((TestScript)Activator.CreateInstance(type)).Name] = type;
                }
            }
            return scripts;
        }

        static Type[] Types(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
        }
    }
}
