using System;
using System.Linq;
using System.Reflection;

namespace VSA.TestSuite.Features.Recording
{
    public static class VideoRecorders
    {
        public static IVideoRecorder Create()
        {
            var port = typeof(IVideoRecorder).Assembly.GetName().Name;
            var type = UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()
                .Where(a => a.GetReferencedAssemblies().Any(r => r.Name == port))
                .SelectMany(Types)
                .FirstOrDefault(IsRecorder);
            return type != null ? (IVideoRecorder)Activator.CreateInstance(type) : new NoVideoRecorder();
        }

        static Type[] Types(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
        }

        static bool IsRecorder(Type type)
            => !type.IsAbstract
                && typeof(IVideoRecorder).IsAssignableFrom(type)
                && type != typeof(NoVideoRecorder)
                && type.GetConstructor(Type.EmptyTypes) != null;
    }
}
