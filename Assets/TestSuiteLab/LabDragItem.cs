using UnityEngine;
using UnityEngine.EventSystems;

namespace TestSuiteLab
{
    public sealed class LabDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int Begins { get; private set; }

        public int Drags { get; private set; }

        public int Ends { get; private set; }

        public void OnBeginDrag(PointerEventData data) => Begins++;

        public void OnDrag(PointerEventData data) => Drags++;

        public void OnEndDrag(PointerEventData data) => Ends++;
    }
}
