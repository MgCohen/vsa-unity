using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VSA.TestSuite.Features.Evidence
{
    public sealed class ScreenshotSystem
    {
        public async Task<string> Take(string folder, string name, CancellationToken cancel)
        {
            await Awaitable.MainThreadAsync();
            if (!Application.isPlaying) return null;
            var path = Path.Combine(folder, name + ".png").Replace('\\', '/');
            ScreenCapture.CaptureScreenshot(path);
            await Awaitable.NextFrameAsync(cancel);
            await Awaitable.NextFrameAsync(cancel);
            if (File.Exists(path)) return path;
            Debug.LogWarning($"No screenshot: the screen capture wrote no file in 2 frames ({path})");
            return null;
        }
    }
}
