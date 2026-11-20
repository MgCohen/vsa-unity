using UnityEngine;
using UnityEngine.EventSystems;

namespace SpikeLab
{
    public sealed class SpikeLabHoldPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        float downTime;

        public int Presses { get; private set; }

        public float HeldSeconds { get; private set; }

        public void OnPointerDown(PointerEventData data)
        {
            Presses++;
            downTime = Time.realtimeSinceStartup;
        }

        public void OnPointerUp(PointerEventData data) => HeldSeconds = Time.realtimeSinceStartup - downTime;
    }
}
