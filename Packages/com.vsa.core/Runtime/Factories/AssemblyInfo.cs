using System.Runtime.CompilerServices;

// The Unity host and the prefab factories of Umpire.Unity register the binder with the same steps as AddFactory.
[assembly: InternalsVisibleTo("Umpire.Unity")]
