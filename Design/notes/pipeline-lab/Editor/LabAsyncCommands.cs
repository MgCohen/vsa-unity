using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Pipeline.Commands;

// Test commands for the command queue: sync, async, background and cancel.
static class LabAsyncCommands
{
    const string Group = "labq";
    static readonly List<string> s_Trace = new List<string>();
    static readonly object s_Lock = new object();

    static string Now() => DateTime.UtcNow.ToString("HH:mm:ss.fff");
    static void Mark(string text) { lock (s_Lock) s_Trace.Add(Now() + " " + text); }

    [CliGroupCommand(Group, "Return at once")]
    internal static ExitCode Ping(CommandContext cmd)
    {
        Mark("ping");
        cmd.Result.WriteRecord("pong " + Now());
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Async: give frames back with Task.Yield for some seconds")]
    internal static async Task<ExitCode> PongYield(CommandContext cmd,
        [CliArg("seconds", "Seconds")] int seconds = 5, [CliArg("tag", "Tag")] string tag = "y")
    {
        Mark(tag + " start");
        var end = DateTime.UtcNow.AddSeconds(seconds);
        try
        {
            while (DateTime.UtcNow < end)
            {
                await Task.Yield();
                cmd.CancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException) { Mark(tag + " cancelled"); throw; }
        Mark(tag + " end");
        cmd.Result.WriteRecord(tag + " done " + Now());
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Async: await Task.Delay with the cancel token")]
    internal static async Task<ExitCode> PongDelay(CommandContext cmd,
        [CliArg("seconds", "Seconds")] int seconds = 5, [CliArg("tag", "Tag")] string tag = "d")
    {
        Mark(tag + " start");
        try { await Task.Delay(seconds * 1000, cmd.CancellationToken); }
        catch (OperationCanceledException) { Mark(tag + " cancelled"); throw; }
        Mark(tag + " end");
        cmd.Result.WriteRecord(tag + " done " + Now());
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Background thread: sleep, checking the cancel token", MainThreadRequired = false)]
    internal static ExitCode PongBackground(CommandContext cmd,
        [CliArg("seconds", "Seconds")] int seconds = 5, [CliArg("tag", "Tag")] string tag = "b")
    {
        Mark(tag + " start thread=" + Thread.CurrentThread.ManagedThreadId);
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < end)
        {
            if (cmd.CancellationToken.IsCancellationRequested) { Mark(tag + " cancelled"); return ExitCode.Cancelled; }
            Thread.Sleep(50);
        }
        Mark(tag + " end");
        cmd.Result.WriteRecord(tag + " done " + Now());
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Sync: block the main thread, ignoring the cancel token")]
    internal static ExitCode PongBlock(CommandContext cmd,
        [CliArg("seconds", "Seconds")] int seconds = 5, [CliArg("tag", "Tag")] string tag = "s")
    {
        Mark(tag + " start");
        Thread.Sleep(seconds * 1000);
        Mark(tag + " end cancelRequested=" + cmd.CancellationToken.IsCancellationRequested);
        cmd.Result.WriteRecord(tag + " done " + Now());
        return ExitCode.Success;
    }

    [CliGroupCommand(Group, "Return and clear the trace", MainThreadRequired = false)]
    internal static ExitCode Trace(CommandContext cmd)
    {
        lock (s_Lock) { foreach (var line in s_Trace) cmd.Result.WriteRecord(line); s_Trace.Clear(); }
        return ExitCode.Success;
    }
}
