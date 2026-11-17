namespace StatusLab
{
    /// <summary>A compile runs.</summary>
    public sealed class Compiling : IEditorState
    {
        public string Name => "compiling";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProbeCompiling == true || now.TruthCompiling == true || (now.BeaconFresh && now.BeaconCompiling == true) || Has(now.ProbeBusyReason, "compil") || Has(now.BusyWindow, "compil");

        static bool Has(string text, string part)
            => text != null && text.Contains(part, StringComparison.OrdinalIgnoreCase);
    }
}
