using System;
using System.Reflection;
using Unity.Pipeline;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class PipelineProgress
    {
        const BindingFlags Internal = BindingFlags.NonPublic | BindingFlags.Instance;
        const string Title = "test session";

        static readonly PropertyInfo CurrentCommand = typeof(CliProgress).Assembly
            .GetType("Unity.Pipeline.Commands.CommandExecutionContext")
            ?.GetProperty("Current", BindingFlags.NonPublic | BindingFlags.Static);

        public static Action<string> KeepCommandTarget()
        {
            var command = CurrentCommand?.GetValue(null);
            var target = command?.GetType().GetProperty("Progress", Internal)?.GetValue(command);
            var report = target?.GetType().GetMethod("Report", Internal);
            if (report == null) return text => CliProgress.Report(Title, text);
            return text => report.Invoke(target, new object[] { Title, text, null, null, null });
        }
    }
}
