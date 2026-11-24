using System;
using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.Objects
{
    internal sealed class AssertStateHandler : RequestHandler<AssertState, StateResult>
    {
        readonly ObjectSystem objects;

        public AssertStateHandler(ObjectSystem objects) => this.objects = objects;

        public override Task<Result<StateResult>> HandleAsync(IEnvelope<AssertState> envelope)
        {
            var assert = envelope.Message;
            var state = objects.State(assert.Path);
            if (state != assert.State)
                throw new InvalidOperationException($"'{assert.Path}' is {state}, expected {assert.State}");
            return Task.FromResult<Result<StateResult>>(new StateResult(assert.Path, state));
        }
    }
}
