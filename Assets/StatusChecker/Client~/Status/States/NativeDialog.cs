namespace VSA.StatusChecker
{
    /// <summary>Fallback of the dialog group: any other dialog window of the own PID.</summary>
    public sealed class NativeDialog : IEditorState
    {
        public string Name => "native-dialog";

        public StateAction Action => StateAction.Answer;

        public bool Matches(Snapshot now, Memory memory)
            => now.DialogTitle != null;
    }
}
