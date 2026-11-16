using UnityEngine;

namespace VSA.TestSuite.Spike
{
    /// <summary>Sends pointer input to the game. A port, because the input stack is external.</summary>
    public interface ITouchDevice
    {
        void Move(Vector2 point);
        void Press(Vector2 point, bool down);
    }
}
