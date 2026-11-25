using System;
using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Features.Objects
{
    internal sealed class AssertTextHandler : RequestHandler<AssertText, TextResult>
    {
        readonly ObjectSystem objects;

        public AssertTextHandler(ObjectSystem objects) => this.objects = objects;

        public override Task<Result<TextResult>> HandleAsync(IEnvelope<AssertText> envelope)
        {
            var assert = envelope.Message;
            var text = objects.Text(assert.Path);
            if (text != assert.Text)
                throw new InvalidOperationException($"'{assert.Path}' text is '{text}', expected '{assert.Text}'");
            return Task.FromResult<Result<TextResult>>(new TextResult(assert.Path, text));
        }
    }
}
