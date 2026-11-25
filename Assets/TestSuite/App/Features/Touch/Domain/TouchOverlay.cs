using System;
using UnityEngine;

namespace VSA.TestSuite.Features.Touch
{
    public sealed class TouchOverlay : IDisposable
    {
        TouchOverlayView view;
        bool on = true;

        public bool On
        {
            get => on;
            set
            {
                on = value;
                if (!on && view != null) view.Clear();
            }
        }

        public void Pressed(Point point)
        {
            if (on) View().Press(new Vector2(point.X, point.Y));
        }

        public void Moved(Point point)
        {
            if (on) View().Move(new Vector2(point.X, point.Y));
        }

        public void Released(Point point)
        {
            if (on) View().Release(new Vector2(point.X, point.Y));
        }

        public void Dispose()
        {
            if (view != null) UnityEngine.Object.Destroy(view.gameObject);
        }

        TouchOverlayView View() => view != null ? view : view = TouchOverlayView.Create();
    }
}
