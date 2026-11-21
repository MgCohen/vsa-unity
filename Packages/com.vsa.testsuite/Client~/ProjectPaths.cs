namespace VSA.TestSuite.Client
{
    /// <summary>Finds the Unity project for the samples, so they run with no arguments inside the repo.</summary>
    public static class ProjectPaths
    {
        /// <summary>
        /// Returns the first folder at or above <paramref name="start"/> (default: the current folder)
        /// that has <c>ProjectSettings/ProjectVersion.txt</c>. Returns null if there is none.
        /// </summary>
        public static string FindProjectRoot(string start = null)
        {
            var dir = new DirectoryInfo(start ?? Directory.GetCurrentDirectory());
            for (; dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "ProjectSettings", "ProjectVersion.txt"))) return dir.FullName;
            return null;
        }

        /// <summary>The project from the first argument, else the project above the current folder.</summary>
        public static string ProjectFromArgs(string[] args)
            => args.Length > 0 ? args[0] : FindProjectRoot()
                ?? throw new InvalidOperationException("No Unity project above " + Directory.GetCurrentDirectory() + ". Give the project path.");
    }
}
