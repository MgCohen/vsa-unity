using System.Collections.Generic;
using Umpire.Markers;

namespace Probe.Posts {
    [AutoRegister(Lifetime.Singleton)]
    public sealed class PostStore {
        public List<Post> Posts { get; } = new List<Post>();
        public int Published { get; set; }
    }

    public sealed record Post(int Id, string Title);
}
