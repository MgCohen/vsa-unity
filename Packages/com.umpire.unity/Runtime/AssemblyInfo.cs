using System.Runtime.CompilerServices;

// The VContainer root scope gives its imports to UnityHost, and claims the app for Shell.Services.
[assembly: InternalsVisibleTo("Umpire.Unity.VContainer")]
