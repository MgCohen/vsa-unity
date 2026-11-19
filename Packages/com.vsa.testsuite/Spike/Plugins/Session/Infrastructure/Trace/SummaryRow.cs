namespace VSA.TestSuite.Spike.Plugins.Session
{
    public sealed record SummaryRow(
        int Index, string Name, string Script, string Status, string Error, float Seconds, string Folder);
}
