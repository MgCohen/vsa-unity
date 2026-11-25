using VSA.StatusChecker;

namespace VSA.StatusChecker.Tests
{
    /// <summary>An IEditorControl that records each call and changes nothing.</summary>
    sealed class FakeControl : IEditorControl
    {
        public readonly List<string> Calls = new();

        /// <summary>The PID that Start gives to the target.</summary>
        public int NextPid = 4242;

        /// <summary>If set, Start throws this exception.</summary>
        public Exception StartFails;

        public bool DialogAnswered = true;

        public Task Start(Target target, CancellationToken cancel)
        {
            Calls.Add("start");
            if (StartFails != null) throw StartFails;
            target.Pid = NextPid;
            return Task.CompletedTask;
        }

        public Task Quit(Target target, CancellationToken cancel)
        {
            Calls.Add("quit");
            return Task.CompletedTask;
        }

        public void CloseWindow(Target target) => Calls.Add("close");

        public void Stop(Target target) => Calls.Add("stop");

        public bool AnswerDialog(Target target, string titlePart, string button)
        {
            Calls.Add("answer " + titlePart + " -> " + button);
            return DialogAnswered;
        }
    }
}
