using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace VSA.TestSuite.Spike.Plugins.Session
{
    public static class TraceFiles
    {
        static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, args) => args.ErrorContext.Handled = true,
        };

        public static string Root()
            => Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestRuns"))
                : Path.Combine(Application.persistentDataPath, "TestRuns");

        public static string NewSessionFolder(string session)
            => Create(Path.Combine(Root(), DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_" + session));

        public static string NewNodeFolder(string session, TraceNode node)
            => Create(Path.Combine(session, node.Index.ToString("000") + "_" + node.Name));

        public static string Write(string folder, string file, object value)
        {
            var path = Path.Combine(folder, file).Replace('\\', '/');
            File.WriteAllText(path, JsonConvert.SerializeObject(value, Json));
            return path;
        }

        static string Create(string folder)
        {
            Directory.CreateDirectory(folder);
            return folder.Replace('\\', '/');
        }
    }
}
