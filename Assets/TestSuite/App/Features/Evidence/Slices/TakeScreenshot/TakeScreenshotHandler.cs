using System;
using System.Threading.Tasks;
using UnityEngine;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Evidence
{
    internal sealed class TakeScreenshotHandler : RequestHandler<TakeScreenshot, ScreenshotResult>
    {
        readonly ScreenshotSystem screenshots;
        readonly TestSession session;

        public TakeScreenshotHandler(ScreenshotSystem screenshots, TestSession session)
        {
            this.screenshots = screenshots;
            this.session = session;
        }

        public override async Task<Result<ScreenshotResult>> HandleAsync(IEnvelope<TakeScreenshot> envelope)
        {
            var name = envelope.Message.Name;
            if (string.IsNullOrEmpty(name)) name = $"screenshot_{Time.frameCount}";
            var path = await screenshots.Take(session.Folder, name, envelope.Cancellation)
                ?? throw new InvalidOperationException("No screenshot: it needs Play Mode or a player");
            return new ScreenshotResult(path);
        }
    }
}
