using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.UnityLinker;

namespace VSA.Unity.Editor {
    // Gives the keep rules of VSA to the UnityLinker (IL2CPP, managed stripping): the link.xml of the package,
    // and one rule for each app class of the project, because VsaHost makes the app class by reflection.
    sealed class VsaLinker : IUnityLinkerProcessor {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data) {
            var path = Path.GetFullPath(Path.Combine("Temp", "VSA", "link.xml"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Only the app classes of the player assemblies: the editor also loads test and editor assemblies.
            var player = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Select(assembly => assembly.name));
            var apps = Apps.All(Apps.Assemblies()).Where(app => player.Contains(app.Assembly.GetName().Name)).ToList();
            File.WriteAllText(path, LinkXml(PackageLinkXml(), apps).ToString());
            UnityEngine.Debug.Log($"VsaLinker: {path} keeps the VSA rules and the app classes: {string.Join(", ", apps.Select(app => app.FullName))}.");
            return path;
        }

        internal static XDocument LinkXml(string package, IEnumerable<Type> apps) {
            var document = XDocument.Parse(package);
            foreach (var group in apps.GroupBy(app => app.Assembly.GetName().Name)) {
                document.Root.Add(new XElement("assembly", new XAttribute("fullname", group.Key),
                    group.Select(app => new XElement("type",
                        new XAttribute("fullname", app.FullName.Replace('+', '/')), new XAttribute("preserve", "all")))));
            }
            return document;
        }

        static string PackageLinkXml() {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(VsaLinker).Assembly);
            return File.ReadAllText(Path.Combine(package.resolvedPath, "Runtime", "link.xml"));
        }
    }
}
