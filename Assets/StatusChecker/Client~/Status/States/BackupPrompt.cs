namespace VSA.StatusChecker
{
    /// <summary>The scene backup dialog. The supervisor presses "Delete".</summary>
    public sealed class BackupPrompt : IEditorState
    {
        public string Name => "backup-prompt";

        public StateAction Action => StateAction.Answer;

        public bool Matches(Snapshot now, Memory memory)
            => Has(now.DialogTitle, "Backup");

        static bool Has(string text, string part)
            => text != null && text.Contains(part, StringComparison.OrdinalIgnoreCase);
    }
}
