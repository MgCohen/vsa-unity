using System.Runtime.CompilerServices;

// The editor tests call UnityVsaHost.OnPlayMode: a test cannot exit the play mode.
[assembly: InternalsVisibleTo("VSA.Unity.Editor.Tests")]
