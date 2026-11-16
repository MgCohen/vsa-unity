using System.Threading.Tasks;
using UnityEngine;
using VSA.Contracts;
using VSA.Handlers.Requests;

namespace VSA.TestSuite.Spike
{
    internal sealed class FindObjectHandler : RequestHandler<FindObject, FindResult>
    {
        readonly ObjectSystem objects;

        public FindObjectHandler(ObjectSystem objects) => this.objects = objects;

        public override async Task<Result<FindResult>> HandleAsync(Envelope<FindObject> envelope)
        {
            await Awaitable.MainThreadAsync();
            var target = objects.Find(envelope.Message.Path);
            return new FindResult(envelope.Message.Path, target.name, objects.ScreenPoint(target));
        }
    }
}
