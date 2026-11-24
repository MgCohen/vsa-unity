namespace VSA.TestSuite.Client.Status
{
    /// <summary>Live time: milliseconds since the recording started.</summary>
    public sealed class WallClock : IClock
    {
        readonly System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        public long NowMs => watch.ElapsedMilliseconds;
    }
}
