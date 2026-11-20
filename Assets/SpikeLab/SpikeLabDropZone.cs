using UnityEngine;
using UnityEngine.EventSystems;

namespace SpikeLab
{
    public sealed class SpikeLabDropZone : MonoBehaviour, IDropHandler
    {
        public int Drops { get; private set; }

        public string Last { get; private set; }

        public void OnDrop(PointerEventData data)
        {
            Drops++;
            Last = data.pointerDrag != null ? data.pointerDrag.name : null;
        }
    }
}
