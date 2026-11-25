using System;
using System.Collections.Generic;
using System.Linq;
using Umpire.Messages.Contracts;
using UnityEngine;

namespace VSA.TestSuite.Plugins.Session
{
    public sealed class TraceRecorder
    {
        readonly TestSession session;
        readonly Dictionary<long, TraceNode> open = new Dictionary<long, TraceNode>();
        readonly List<SummaryRow> rows = new List<SummaryRow>();
        readonly object gate = new object();
        int count;

        public TraceRecorder(TestSession session) => this.session = session;

        public TraceNode Last { get; private set; }

        public bool Closed { get; private set; }

        public string LastFile => Last != null ? Last.Folder + "/trace.json" : null;

        public void Start()
        {
            session.Folder = TraceFiles.NewSessionFolder(session.Id);
            Application.logMessageReceivedThreaded += OnLog;
            WriteSummary(null);
        }

        public TraceNode Begin(Envelope envelope)
        {
            var node = new TraceNode
            {
                EnvelopeId = envelope.Id,
                Name = envelope.Message.GetType().Name,
                Message = envelope.Message,
                Depth = envelope.Depth,
                StartFrame = Time.frameCount,
                StartTime = Time.realtimeSinceStartup,
            };
            var parent = ParentOf(envelope);
            if (parent == null)
                OpenEntry(node, envelope.Message is IScriptRun);
            else
                OpenChild(parent, node);
            lock (gate) open[node.EnvelopeId] = node;
            return node;
        }

        public TraceNode Finish(Envelope envelope)
        {
            TraceNode node;
            lock (gate)
            {
                if (!open.TryGetValue(envelope.Id, out node)) return null;
                open.Remove(envelope.Id);
            }
            node.Frames = Time.frameCount - node.StartFrame;
            node.Seconds = Time.realtimeSinceStartup - node.StartTime;
            node.Error = envelope.Exception?.Message ?? envelope.Error?.ToString();
            node.Status = node.Error == null ? TraceNode.Ok : TraceNode.Failed;
            node.Result = envelope.Result?.Value;
            Close(node);
            return node;
        }

        public void EndOpen()
        {
            List<TraceNode> nodes;
            lock (gate)
            {
                nodes = open.Values.OrderByDescending(n => n.Depth).ToList();
                open.Clear();
            }
            foreach (var node in nodes)
            {
                node.Status = TraceNode.EndedBySession;
                node.Frames = Time.frameCount - node.StartFrame;
                node.Seconds = Time.realtimeSinceStartup - node.StartTime;
                Close(node);
            }
            if (Last != null && nodes.Count > 0) TraceFiles.Write(Last.Folder, "trace.json", Last);
        }

        public void End(string reason)
        {
            if (Closed) return;
            Closed = true;
            Application.logMessageReceivedThreaded -= OnLog;
            WriteSummary(reason);
        }

        TraceNode ParentOf(Envelope envelope)
        {
            lock (gate)
            {
                if (envelope.Parent != null && open.TryGetValue(envelope.Parent.Id, out var parent)) return parent;
                return open.Values.Where(n => n.Depth == envelope.Depth - 1).OrderBy(n => n.StartTime).LastOrDefault();
            }
        }

        void OpenEntry(TraceNode node, bool script)
        {
            node.Index = ++count;
            node.IsStep = !script;
            node.Folder = TraceFiles.NewNodeFolder(session.Folder, node);
            Last = node;
        }

        void OpenChild(TraceNode parent, TraceNode node)
        {
            parent.Children.Add(node);
            if (!(parent.Message is IScriptRun run) || parent.Depth != 0) return;
            node.Index = ++count;
            node.IsStep = true;
            node.Script = run.Name;
            node.Folder = TraceFiles.NewNodeFolder(session.Folder, node);
        }

        void Close(TraceNode node)
        {
            if (node.IsStep) CloseStep(node);
            if (node == Last) TraceFiles.Write(node.Folder, "trace.json", node);
        }

        void CloseStep(TraceNode node)
        {
            TraceFiles.Write(node.Folder, "step.json", node);
            List<ConsoleLine> lines;
            lock (gate) lines = node.Console.ToList();
            if (node.Status != TraceNode.Ok || lines.Any(l => l.Level != nameof(LogType.Log)))
                TraceFiles.Write(node.Folder, "console.json", lines);
            rows.Add(new SummaryRow(
                node.Index, node.Name, node.Script, node.Status, node.Error, node.Seconds, node.Folder));
            WriteSummary(null);
        }

        void WriteSummary(string endReason)
        {
            var failed = rows.Count(r => r.Status != TraceNode.Ok);
            TraceFiles.Write(session.Folder, "summary.json", new
            {
                session = session.Id,
                started = session.StartedUtc,
                ended = endReason != null ? DateTime.UtcNow : (DateTime?)null,
                endReason,
                steps = rows.Count,
                failed,
                rows,
            });
        }

        void OnLog(string message, string stack, LogType type)
        {
            var line = new ConsoleLine(type.ToString(), message, type == LogType.Log ? null : stack);
            lock (gate)
            {
                foreach (var node in open.Values.Where(n => n.IsStep)) node.Console.Add(line);
            }
        }
    }
}
