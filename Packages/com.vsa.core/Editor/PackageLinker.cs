using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace Umpire.Unity.Editor {
    // Gives Runtime/link.xml of the package to the UnityLinker (IL2CPP, managed stripping).
    // The UnityLinker reads the link.xml files of Assets/ only.
    sealed class PackageLinker : IUnityLinkerProcessor {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data) {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PackageLinker).Assembly);
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(package.resolvedPath, "Runtime", "link.xml"));
        }
    }
}
