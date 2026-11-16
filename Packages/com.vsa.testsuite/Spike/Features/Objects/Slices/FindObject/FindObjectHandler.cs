using System.Threading.Tasks;

namespace VSA.TestSuite.Spike
{
    internal sealed class FindObjectHandler : ActionHandler<FindObject, FindResult>
    {
        readonly ObjectSystem objects;

        public FindObjectHandler(ObjectSystem objects) => this.objects = objects;

        protected override Task<FindResult> Handle(FindObject action, ActionContext context)
        {
            var target = objects.Find(action.Path);
            var point = objects.ScreenPoint(target);
            return Task.FromResult(new FindResult(action.Path, target.name, point));
        }
    }
}
