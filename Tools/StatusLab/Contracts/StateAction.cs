namespace StatusLab
{
    /// <summary>What the supervisor does for a state.</summary>
    public enum StateAction
    {
        None,
        Wait,
        Retry,
        Answer,
        Escalate,
        Fail,
        Start,
    }
}
