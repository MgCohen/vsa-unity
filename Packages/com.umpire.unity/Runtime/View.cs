using Umpire.Unity.Factories;

namespace Umpire.Unity {
    // A view of one .NET object, for example a view controller. The app binds the views of each loaded scene, and a
    // prefab factory binds the views of each copy. A view never resolves its target.
    //   public sealed class RollListView : View<RollListController> { ... }
    public abstract class View<T> : Bound<T> where T : class {
    }
}
