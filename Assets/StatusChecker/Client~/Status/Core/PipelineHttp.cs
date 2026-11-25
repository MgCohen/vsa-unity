using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VSA.StatusChecker
{
    /// <summary>Direct HTTP to the Pipeline server. Returns the HTTP status and the body. Never throws for a lost server.</summary>
    public static class PipelineHttp
    {
        static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

        /// <summary>HTTP status 0: no connection. -1: no answer in time.</summary>
        public static async Task<(int Http, string Body, double Ms)> Get(Target target, string path, int timeoutMs, CancellationToken cancel)
        {
            if (target.Port <= 0) return (0, null, 0);
            using var req = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:" + target.Port + path);
            return await Send(target, req, timeoutMs, cancel);
        }

        public static async Task<(int Http, string Body, double Ms)> Exec(Target target, string[] argv, int timeoutMs, CancellationToken cancel)
        {
            if (target.Port <= 0) return (0, null, 0);
            var body = JsonSerializer.Serialize(new { argv, timeout = timeoutMs });
            using var req = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:" + target.Port + "/api/exec")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            // The server drops a request that does not start within "timeout". Wait a bit longer for its 504.
            return await Send(target, req, timeoutMs + 1000, cancel);
        }

        static async Task<(int, string, double)> Send(Target target, HttpRequestMessage req, int timeoutMs, CancellationToken cancel)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", target.Token);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeout.CancelAfter(timeoutMs);
            try
            {
                using var resp = await Http.SendAsync(req, timeout.Token);
                var text = await resp.Content.ReadAsStringAsync(timeout.Token);
                return ((int)resp.StatusCode, text, watch.Elapsed.TotalMilliseconds);
            }
            catch (HttpRequestException)
            {
                return (0, null, watch.Elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (!cancel.IsCancellationRequested)
            {
                return (-1, null, watch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
