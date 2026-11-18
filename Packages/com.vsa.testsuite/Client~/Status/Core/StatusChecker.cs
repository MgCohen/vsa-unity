namespace VSA.TestSuite.Client.Status
{
    /// <summary>Pure classification: the first state in States.All that matches. It changes nothing.</summary>
    public static class StatusChecker
    {
        public static IEditorState Check(Snapshot now, Memory memory)
        {
            foreach (var state in States.All)
                if (state.Matches(now, memory)) return state;
            throw new InvalidOperationException("No state matches. States.All must end with fallback states.");
        }
    }
}
