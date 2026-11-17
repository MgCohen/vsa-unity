namespace StatusLab
{
    /// <summary>An asset import or refresh runs.</summary>
    public sealed class Importing : IEditorState
    {
        public string Name => "importing";

        public StateAction Action => StateAction.Wait;

        public bool Matches(Snapshot now, Memory memory)
            => now.ProbeUpdating == true || (now.BeaconFresh && now.BeaconUpdating == true) || Has(now.ProbeBusyReason, "import") || Has(now.ProbeBusyReason, "refresh") || Has(now.BusyWindow, "import");

        static bool Has(string text, string part)
            => text != null && text.Contains(part, StringComparison.OrdinalIgnoreCase);
    }
}
