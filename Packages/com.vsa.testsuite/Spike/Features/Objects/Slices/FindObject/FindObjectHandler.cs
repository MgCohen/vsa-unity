using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace VSA.TestSuite.Spike.Features.Objects
{
    internal sealed class FindObjectHandler : RequestHandler<FindObject, FindResult>
    {
        readonly ObjectSystem objects;

        public FindObjectHandler(ObjectSystem objects) => this.objects = objects;

        public override Task<Result<FindResult>> HandleAsync(Envelope<FindObject> envelope)
        {
            var target = objects.Find(envelope.Message.Path);
            var result = new FindResult(envelope.Message.Path, target.name, objects.ScreenPoint(target));
            return Task.FromResult<Result<FindResult>>(result);
        }
    }
}
