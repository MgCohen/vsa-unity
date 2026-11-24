using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>The client side of the daemon pipe: one request line, one reply line.</summary>
    public static class DaemonClient
    {
        /// <summary>Sends a request to the daemon of the project. Returns null if no daemon answers in
        /// <paramref name="connectTimeoutMs"/>.</summary>
        public static async Task<DaemonReply> Send(string projectPath, DaemonRequest request, int connectTimeoutMs, CancellationToken cancel = default)
        {
            using var pipe = new NamedPipeClientStream(".", DaemonPaths.PipeName(projectPath), PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            try
            {
                await pipe.ConnectAsync(connectTimeoutMs, cancel);
            }
            catch (TimeoutException)
            {
                return null;
            }
            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, leaveOpen: true);
            await writer.WriteLineAsync(JsonSerializer.Serialize(request, Recorder.Options).AsMemory(), cancel);
            var line = await reader.ReadLineAsync(cancel);
            if (line == null) throw new IOException("The daemon closed the pipe before it answered.");
            return JsonSerializer.Deserialize<DaemonReply>(line, Recorder.Options);
        }

        /// <summary>True if a daemon answers on the pipe of the project.</summary>
        public static async Task<bool> Running(string projectPath, CancellationToken cancel = default)
            => await Send(projectPath, new DaemonRequest { Command = "status" }, 300, cancel) != null;

        /// <summary>Starts the daemon process if none answers, then waits until it answers (up to 15 s).
        /// The daemon runs on after the caller exits. It gets no console handles of the caller.</summary>
        public static async Task<bool> Ensure(string projectPath, ProcessStartInfo daemon, CancellationToken cancel = default)
        {
            if (await Running(projectPath, cancel)) return false;
            daemon.UseShellExecute = true;
            daemon.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(daemon)?.Dispose();
            var until = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < until)
            {
                if (await Running(projectPath, cancel)) return true;
                await Task.Delay(250, cancel);
            }
            throw new TimeoutException("The daemon did not answer in 15 s. Read " + Path.Combine(projectPath, "Library", "VSA.TestSuite", "Status", "daemon.log") + ".");
        }
    }
}
