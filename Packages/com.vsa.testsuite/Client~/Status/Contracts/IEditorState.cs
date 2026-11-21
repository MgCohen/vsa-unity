namespace VSA.TestSuite.Client.Status
{
    /// <summary>One Editor state. Add a state: one file in States/ and one entry in States.All.</summary>
    public interface IEditorState
    {
        /// <summary>The state name, for example "frozen". Keys limits.json.</summary>
        string Name { get; }

        /// <summary>True if this state fits the snapshot. Earlier states in States.All win.</summary>
        bool Matches(Snapshot now, Memory memory);

        StateAction Action { get; }
    }
}
