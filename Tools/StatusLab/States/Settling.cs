namespace StatusLab
{
    /// <summary>The server answers "settling": imports and compiles of the start.</summary>
    public sealed class Settling : IEditorState
    {
        public string Name => "settling";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => now.Status == "settling";
    }
}
