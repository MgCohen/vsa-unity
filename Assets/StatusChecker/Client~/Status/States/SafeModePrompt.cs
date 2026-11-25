namespace VSA.StatusChecker
{
    /// <summary>The "Enter Safe Mode?" dialog. The supervisor presses "Ignore".</summary>
    public sealed class SafeModePrompt : IEditorState
    {
        public string Name => "safe-mode-prompt";

        public StateAction Action => StateAction.Answer;

        public bool Matches(Snapshot now, Memory memory)
            => Has(now.DialogTitle, "Safe Mode");

        static bool Has(string text, string part)
            => text != null && text.Contains(part, StringComparison.OrdinalIgnoreCase);
    }
}
