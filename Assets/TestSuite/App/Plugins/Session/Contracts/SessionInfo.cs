namespace VSA.TestSuite.Plugins.Session
{
    public sealed record SessionInfo(
        string Platform,
        bool IsEditor,
        bool IsPlaying,
        int Frame,
        double Time,
        string Scene,
        int ProcessId,
        string Session);
}
