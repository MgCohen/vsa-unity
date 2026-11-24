using System.Threading.Tasks;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace VSA.TestSuite.Spike.Features.Objects
{
    internal sealed class LocateObjectHandler : CommandHandler<LocateObject, ObjectPoint>
    {
        readonly ObjectSystem objects;

        public LocateObjectHandler(ObjectSystem objects) => this.objects = objects;

        public override Task<Result<ObjectPoint>> HandleAsync(IEnvelope<LocateObject> envelope)
        {
            var target = objects.Find(envelope.Message.Path);
            var located = new ObjectPoint(envelope.Message.Path, target.name, objects.ScreenPoint(target));
            return Task.FromResult<Result<ObjectPoint>>(located);
        }
    }
}
