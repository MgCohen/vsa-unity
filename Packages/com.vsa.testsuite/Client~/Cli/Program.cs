using VSA.TestSuite.Cli;
using VSA.TestSuite.Client.Status;

var options = new Options(args);
Output.Human = options.Get("--format") == "human";
var verbs = options.Verbs;
var command = string.Join(" ", verbs.Take(verbs.FirstOrDefault() == "log" || verbs.FirstOrDefault() == "config" ? 2 : 1));

using var cancelSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancelSource.Cancel();
};

try
{
    return verbs.FirstOrDefault() switch
    {
        "status" => await Commands.Status(options, cancelSource.Token),
        "wait" => await Commands.Wait(options, cancelSource.Token),
        "open" => await Commands.Open(options, cancelSource.Token),
        "close" => await Commands.Close(options, cancelSource.Token),
        "daemon" => await Commands.Daemon(options, cancelSource.Token),
        "log" => Commands.Log(options, verbs.Length > 1 ? verbs[1] : null),
        "config" when verbs.Length > 1 && verbs[1] == "show" => Commands.Config(options),
        "help" or null => Output.Envelope("help", new { text = Commands.Help }),
        _ => Output.Envelope(command, new { text = Commands.Help }, "BadArguments", "Unknown command '" + command + "'."),
    };
}
catch (ArgumentException e)
{
    return Output.Envelope(command, null, "BadArguments", e.Message);
}
catch (DirectoryNotFoundException e)
{
    return Output.Envelope(command, null, "NotAProject", e.Message);
}
catch (FileNotFoundException e)
{
    return Output.Envelope(command, null, "NotFound", e.Message);
}
catch (StartRefusedException e)
{
    return Output.Envelope(command, new { errors = e.Errors, log = e.LogPath }, "StartRefused", e.Message);
}
catch (Exception e) when (e is IOException or TimeoutException)
{
    return Output.Envelope(command, null, "DaemonFailed", e.Message);
}
catch (OperationCanceledException)
{
    return Output.Envelope(command, null, "Cancelled", "The command was cancelled.");
}
