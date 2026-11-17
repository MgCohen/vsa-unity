namespace StatusLab
{
    /// <summary>The Editor did not start with -automated. Its state is not trusted.</summary>
    public sealed class NotAutomated : IEditorState
    {
        public string Name => "not-automated";

        public StateAction Action => StateAction.Fail;

        public bool Matches(Snapshot now, Memory memory)
            => now.LogIsHuman == 1 || now.LogStartBugReporter == 1 || (now.DescPid == memory.OwnPid && now.DescInfoSet);
    }
}
