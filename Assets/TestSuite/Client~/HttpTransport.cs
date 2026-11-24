using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VSA.TestSuite.Client
{
    /// <summary>The answer of one command.</summary>
    public sealed class ExecResult
    {
        public int Http;
        public bool Success;
        /// <summary>The "result" value of the response. For our actions it has ok, action, step, ms and result.</summary>
        public JsonElement Result;
        /// <summary>The Pipeline error, for example "Not Found" or "Unknown command".</summary>
        public string Error;
        public string ErrorDetails;
        public double Ms;

        public override string ToString() => "HTTP " + Http + " " + (Success ? "ok" : Error + ": " + ErrorDetails);
    }

    /// <summary>The way to send commands to an instance. One interface for all transports.</summary>
    public interface ITransport
    {
        Instance Instance { get; }
        Task<ExecResult> Exec(string[] argv, int timeoutMs, CancellationToken cancel = default);
        Task<JsonElement?> Get(string endpoint, int timeoutMs, CancellationToken cancel = default);
    }

    /// <summary>
    /// Direct HTTP to the Pipeline server, about 20 ms for each call. It reads the descriptor
    /// again when the connection fails, because a domain reload or a restart changes the port and token.
    /// </summary>
    public sealed class HttpTransport : ITransport, IDisposable
    {
        readonly HttpClient http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        public Instance Instance { get; }

        /// <summary>How long to wait for the server after a lost connection, for example during a domain reload.</summary>
        public int ReconnectMs { get; set; } = 60000;

        /// <summary>How long to retry when the server answers 503 (not ready).</summary>
        public int NotReadyMs { get; set; } = 60000;

        public HttpTransport(Instance instance) => Instance = instance;

        public async Task<ExecResult> Exec(string[] argv, int timeoutMs, CancellationToken cancel = default)
        {
            var body = ExecBody(argv, timeoutMs);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var reconnectUntil = DateTime.UtcNow.AddMilliseconds(ReconnectMs);
            var notReadyUntil = DateTime.UtcNow.AddMilliseconds(NotReadyMs);

            while (true)
            {
                await EnsureLoaded(reconnectUntil, cancel);
                using var req = Request(HttpMethod.Post, "/api/exec");
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
                timeout.CancelAfter(timeoutMs + 10000);

                HttpResponseMessage resp;
                try
                {
                    resp = await http.SendAsync(req, timeout.Token);
                }
                catch (HttpRequestException)
                {
                    // Connection lost: a domain reload, a restart, or a crash.
                    if (DateTime.UtcNow > reconnectUntil) throw new InstanceUnreachableException(Instance, "No connection for " + ReconnectMs + " ms");
                    Instance.TryLoad();
                    await Task.Delay(500, cancel);
                    continue;
                }
                catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
                {
                    throw new TimeoutException("No answer to '" + string.Join(" ", argv) + "' in " + (timeoutMs + 10000) + " ms");
                }

                using (resp)
                {
                    var text = await resp.Content.ReadAsStringAsync(cancel);
                    if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        if (text.Contains("blocked_by_dialog")) throw new DialogOpenException(Instance, text);
                        if (DateTime.UtcNow > notReadyUntil) throw new NotReadyException(Instance, text);
                        await Task.Delay(300, cancel);
                        continue;
                    }
                    return Parse((int)resp.StatusCode, text, watch.Elapsed.TotalMilliseconds);
                }
            }
        }

        public async Task<JsonElement?> Get(string endpoint, int timeoutMs, CancellationToken cancel = default)
        {
            if (!Instance.TryLoad()) return null;
            using var req = Request(HttpMethod.Get, endpoint);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeout.CancelAfter(timeoutMs);
            using var resp = await http.SendAsync(req, timeout.Token);
            var text = await resp.Content.ReadAsStringAsync(cancel);
            return string.IsNullOrWhiteSpace(text) ? null : JsonDocument.Parse(text).RootElement.Clone();
        }

        HttpRequestMessage Request(HttpMethod method, string endpoint)
        {
            var req = new HttpRequestMessage(method, "http://127.0.0.1:" + Instance.Port + endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Instance.Token);
            return req;
        }

        async Task EnsureLoaded(DateTime until, CancellationToken cancel)
        {
            while (!Instance.TryLoad())
            {
                if (DateTime.UtcNow > until) throw new InstanceUnreachableException(Instance, "No descriptor at " + Instance.DescriptorPath);
                await Task.Delay(500, cancel);
            }
        }

        // Writes {"argv":[...],"timeout":n} with no reflection, so trimmed and AOT apps can use the client.
        static string ExecBody(string[] argv, int timeoutMs)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream))
            {
                w.WriteStartObject();
                w.WriteStartArray("argv");
                foreach (var a in argv) w.WriteStringValue(a);
                w.WriteEndArray();
                w.WriteNumber("timeout", timeoutMs);
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        static ExecResult Parse(int http, string text, double ms)
        {
            var r = new ExecResult { Http = http, Ms = Math.Round(ms, 1) };
            if (string.IsNullOrWhiteSpace(text)) { r.Error = "Empty response"; return r; }
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            r.Success = root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True;
            if (root.TryGetProperty("result", out var result)) r.Result = result.Clone();
            if (root.TryGetProperty("error", out var e)) r.Error = e.ToString();
            if (root.TryGetProperty("errorDetails", out var d)) r.ErrorDetails = d.ToString();
            return r;
        }

        public void Dispose() => http.Dispose();
    }

    public class InstanceException : Exception
    {
        public Instance Instance { get; }
        public InstanceException(Instance instance, string message) : base(message + " (" + instance + ")") => Instance = instance;
    }

    public sealed class InstanceUnreachableException : InstanceException
    {
        public InstanceUnreachableException(Instance i, string m) : base(i, m) { }
    }

    public sealed class NotReadyException : InstanceException
    {
        public NotReadyException(Instance i, string m) : base(i, "The instance stays not ready: " + m) { }
    }

    public sealed class DialogOpenException : InstanceException
    {
        public DialogOpenException(Instance i, string m) : base(i, "A modal dialog blocks the main thread: " + m) { }
    }
}
