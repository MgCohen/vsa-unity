using System.Runtime.CompilerServices;

// The Unity factories and the Unity host register the binder with the same steps as AddFactory.
[assembly: InternalsVisibleTo("Umpire.Unity.Factories")]
[assembly: InternalsVisibleTo("Umpire.Unity")]
