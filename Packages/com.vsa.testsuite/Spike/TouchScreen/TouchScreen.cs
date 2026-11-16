using UnityEngine;

namespace VSA.TestSuite.Spike
{
    internal sealed record TouchScreen(Vector2 Point) : IAction<TouchResult>;
}
