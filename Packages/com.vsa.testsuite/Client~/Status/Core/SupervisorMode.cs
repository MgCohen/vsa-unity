namespace VSA.TestSuite.Client.Status
{
    /// <summary>Off: no action. DryRun: log what it would do. Observe: answer dialogs only; never close, stop or start.
    /// Live: act.</summary>
    public enum SupervisorMode
    {
        Off,
        DryRun,
        Observe,
        Live,
    }
}
