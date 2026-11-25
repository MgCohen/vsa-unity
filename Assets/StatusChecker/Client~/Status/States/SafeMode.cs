namespace VSA.StatusChecker
{
    /// <summary>The Editor runs in Safe Mode: in -automated, a start with compile errors enters it with no dialog. Close it, never restart.</summary>
    public sealed class SafeMode : IEditorState
    {
        public string Name => "safe-mode";

        public StateAction Action => StateAction.Fail;

        public bool Matches(Snapshot now, Memory memory)
            => Has(now.MainTitle, "SAFE MODE") || Has(now.EditorsPhase, "safe");

        static bool Has(string text, string part)
            => text != null && text.Contains(part, StringComparison.OrdinalIgnoreCase);
    }
}
