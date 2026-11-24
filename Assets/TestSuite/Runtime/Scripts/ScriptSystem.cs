using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VSA.TestSuite
{
    /// <summary>
    /// A test script in C#. It runs inside Unity, and each call on the script context is one step
    /// with its own evidence. Start it with "suite.run --script Name".
    /// </summary>
    public interface ITestScript
    {
        /// <summary>The name for suite.run. Use a short name with no spaces.</summary>
        string Name { get; }
        Task Run(ScriptContext s);
    }

    /// <summary>
    /// The DSL of a script: one method for each built-in action. For your own actions, call
    /// <see cref="Run{T}"/> or write extension methods on this class.
    /// </summary>
    public sealed class ScriptContext
    {
        readonly ActionRuntime runtime;

        public string Script { get; }
        public CancellationToken Cancel { get; }
        public List<string> Steps { get; } = new List<string>();

        public ScriptContext(ActionRuntime runtime, string script, CancellationToken cancel)
        {
            this.runtime = runtime;
            Script = script;
            Cancel = cancel;
        }

        /// <summary>Runs any action as one step of this script.</summary>
        public Task<T> Run<T>(IAction<T> action)
        {
            Steps.Add(ActionNames.Of(action.GetType()));
            return runtime.RunStep(action, Cancel, Script);
        }

        public Task<ClickResult> ClickObject(string path) => Run(new ClickObject(path));
        public Task<ClickResult> ClickScreen(float x, float y) => Run(new ClickScreen(x, y));
        public Task<ClickResult> HoldObject(string path, int ms) => Run(new HoldObject(path, ms));
        public Task<DragResult> DragObject(string path, string toPath, int steps = 10) => Run(new DragObject(path, toPath, steps));
        public Task<ObjectRef> FindObject(string path) => Run(new FindObject(path));
        public Task<WaitResult> WaitForObject(string path, ObjectState state = ObjectState.Active, int timeoutMs = 10000)
            => Run(new WaitForObject(path, state, timeoutMs));
        public Task<ObjectState> AssertState(string path, ObjectState state = ObjectState.Active) => Run(new AssertState(path, state));
        public Task<string> AssertText(string path, string text) => Run(new AssertText(path, text));
        public Task<WaitResult> WaitFrames(int count) => Run(new WaitFrames(count));
        public Task<WaitResult> WaitSeconds(double seconds) => Run(new WaitSeconds(seconds));
        public Task<SceneResult> LoadScene(string scene) => Run(new LoadScene(scene));
        public Task<SceneResult> Reset(string scene = null, int? seed = null) => Run(new ResetGame(scene, seed));
    }

    /// <summary>The result of a script.</summary>
    public sealed class ScriptResult
    {
        public string Script;
        public int Steps;
        public double Ms;
        public string RunFolder;
    }

    /// <summary>Finds test scripts and runs them. Scripts are found by type, or added in the composition root.</summary>
    public sealed class ScriptSystem
    {
        readonly Dictionary<string, ITestScript> added = new Dictionary<string, ITestScript>(StringComparer.OrdinalIgnoreCase);
        ActionRuntime runtime;
        Dictionary<string, Type> found;

        /// <summary>The composition root calls this method after it builds the runtime.</summary>
        public void Attach(ActionRuntime runtime) => this.runtime = runtime;

        public ScriptSystem Add(ITestScript script)
        {
            added[script.Name] = script;
            return this;
        }

        public string[] Names() => added.Keys.Concat(Found().Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToArray();

        public async Task<ScriptResult> Run(string name, CancellationToken cancel)
        {
            if (runtime == null) throw new ActionFailed(FailCode.Failed, "The script system has no runtime. Call Attach in the composition root.");
            var script = Create(name)
                ?? throw new ActionFailed(FailCode.NotFound, "No test script '" + name + "'", new { scripts = Names() });

            var s = new ScriptContext(runtime, script.Name, cancel);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await script.Run(s);
            }
            catch (ActionFailed f)
            {
                throw new ActionFailed(f.Code, "Script '" + script.Name + "' stopped: " + f.Message,
                    new { script = script.Name, runFolder = runtime.Run.Path, cause = f.Detail });
            }
            return new ScriptResult
            {
                Script = script.Name,
                Steps = s.Steps.Count,
                Ms = Math.Round(watch.Elapsed.TotalMilliseconds, 1),
                RunFolder = runtime.Run.Path,
            };
        }

        ITestScript Create(string name)
        {
            if (added.TryGetValue(name, out var script)) return script;
            return Found().TryGetValue(name, out var type) ? (ITestScript)Activator.CreateInstance(type) : null;
        }

        // All classes that implement ITestScript and have a constructor with no parameters.
        Dictionary<string, Type> Found()
        {
            if (found != null) return found;
            found = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                foreach (var t in types)
                {
                    if (t.IsAbstract || t.IsInterface || !typeof(ITestScript).IsAssignableFrom(t) || t.GetConstructor(Type.EmptyTypes) == null) continue;
                    var instance = (ITestScript)Activator.CreateInstance(t);
                    found[instance.Name] = t;
                }
            }
            return found;
        }
    }
}
