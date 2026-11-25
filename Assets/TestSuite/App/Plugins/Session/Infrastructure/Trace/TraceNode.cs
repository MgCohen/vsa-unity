using System.Collections.Generic;
using Newtonsoft.Json;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed class TraceNode
    {
        public const string Running = "running";
        public const string Ok = "ok";
        public const string Failed = "failed";
        public const string EndedBySession = "ended by session end";

        public int Index { get; set; }

        public string Name { get; set; }

        public object Message { get; set; }

        public int Depth { get; set; }

        public bool IsStep { get; set; }

        public string Script { get; set; }

        public string Status { get; set; } = Running;

        public string Error { get; set; }

        public object Result { get; set; }

        public int StartFrame { get; set; }

        public int Frames { get; set; }

        public float Seconds { get; set; }

        public List<TraceNode> Children { get; } = new List<TraceNode>();

        [JsonIgnore]
        public long EnvelopeId { get; set; }

        [JsonIgnore]
        public float StartTime { get; set; }

        [JsonIgnore]
        public string Folder { get; set; }

        [JsonIgnore]
        public List<ConsoleLine> Console { get; } = new List<ConsoleLine>();
    }
}
