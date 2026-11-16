namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// A screen point in pixels from the lower left. Features share it.
    /// It has no Unity type, so the CLI can write it as JSON.
    /// </summary>
    public readonly struct Point
    {
        public float X { get; }
        public float Y { get; }

        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
    }
}
