using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace VSA.TestSuite
{
    /// <summary>The look of the touch overlay. Change it in the composition root.</summary>
    public sealed class TouchOverlayOptions
    {
        public bool Enabled = true;
        public Color Color = new Color(1f, 0.25f, 0.2f, 1f);
        /// <summary>The diameter of the dot under the pointer while it is down, in pixels.</summary>
        public float DotSize = 32;
        /// <summary>The largest diameter of the burst at a press, in pixels.</summary>
        public float BurstSize = 96;
        /// <summary>The life of a burst, in seconds.</summary>
        public float BurstSeconds = 0.4f;
        /// <summary>The life of one trail dot of a drag, in seconds. 0: no trail.</summary>
        public float TrailSeconds = 0.5f;
    }

    /// <summary>
    /// A pointer hook that shows the test input on the screen, as in a tutorial video:
    /// a burst at each press, a dot while the pointer is down, and a fading trail for a drag.
    /// The overlay takes no raycasts, so it does not change what the input hits.
    /// </summary>
    public sealed class TouchOverlayHook : IPointerHook
    {
        TouchOverlayView view;

        public TouchOverlayOptions Options { get; }

        public TouchOverlayHook(TouchOverlayOptions options = null) => Options = options ?? new TouchOverlayOptions();

        public bool Enabled
        {
            get => Options.Enabled;
            set
            {
                Options.Enabled = value;
                if (!value && view != null) view.Clear();
            }
        }

        public void Pressed(Vector2 p)
        {
            if (!Ready()) return;
            view.Burst(p, Options.BurstSize, Options.BurstSeconds, 0.35f);
            view.ShowDot(p);
        }

        public void Moved(Vector2 p, bool down)
        {
            if (!down || !Ready()) return;
            view.ShowDot(p);
            if (Options.TrailSeconds > 0) view.Trail(p, Options.TrailSeconds);
        }

        public void Released(Vector2 p)
        {
            if (!Ready()) return;
            view.HideDot();
            view.Burst(p, Options.DotSize * 1.5f, Options.BurstSeconds * 0.6f, 0.6f);
        }

        bool Ready()
        {
            if (!Options.Enabled || !Application.isPlaying) return false;
            if (view == null) view = TouchOverlayView.Create(Options);
            return true;
        }
    }

    /// <summary>The canvas and the animation of the touch overlay. It lives across scene loads.</summary>
    public sealed class TouchOverlayView : MonoBehaviour
    {
        sealed class Fade
        {
            public Image Image;
            public float Age, Life, StartSize, EndSize, StartAlpha;
        }

        readonly List<Fade> fades = new List<Fade>();
        readonly Stack<Image> pool = new Stack<Image>();
        TouchOverlayOptions options;
        Sprite disc, ring;
        Image dot;

        internal static TouchOverlayView Create(TouchOverlayOptions options)
        {
            var go = new GameObject("VSA.TouchOverlay", typeof(Canvas));
            DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var view = go.AddComponent<TouchOverlayView>();
            view.options = options;
            view.disc = MakeSprite(filled: true);
            view.ring = MakeSprite(filled: false);
            view.dot = view.NewImage(view.disc);
            view.dot.gameObject.SetActive(false);
            return view;
        }

        public void ShowDot(Vector2 p)
        {
            Place(dot, p, options.DotSize);
            dot.color = WithAlpha(options.Color, 0.85f);
            dot.gameObject.SetActive(true);
            dot.transform.SetAsLastSibling();
        }

        public void HideDot() => dot.gameObject.SetActive(false);

        public void Burst(Vector2 p, float size, float seconds, float startScale)
            => Add(ring, p, size * startScale, size, seconds, 0.9f);

        public void Trail(Vector2 p, float seconds)
            => Add(disc, p, options.DotSize * 0.45f, options.DotSize * 0.2f, seconds, 0.6f);

        public void Clear()
        {
            foreach (var f in fades) Release(f.Image);
            fades.Clear();
            HideDot();
        }

        void Add(Sprite sprite, Vector2 p, float startSize, float endSize, float life, float alpha)
        {
            var img = pool.Count > 0 ? pool.Pop() : NewImage(sprite);
            img.sprite = sprite;
            img.gameObject.SetActive(true);
            Place(img, p, startSize);
            img.color = WithAlpha(options.Color, alpha);
            fades.Add(new Fade { Image = img, Life = Mathf.Max(0.01f, life), StartSize = startSize, EndSize = endSize, StartAlpha = alpha });
        }

        // Unscaled time: the overlay must animate also when the game sets timeScale to 0.
        void Update()
        {
            for (int i = fades.Count - 1; i >= 0; i--)
            {
                var f = fades[i];
                f.Age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(f.Age / f.Life);
                float ease = 1f - (1f - t) * (1f - t);
                f.Image.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(f.StartSize, f.EndSize, ease);
                f.Image.color = WithAlpha(options.Color, f.StartAlpha * (1f - t));
                if (t >= 1f)
                {
                    Release(f.Image);
                    fades.RemoveAt(i);
                }
            }
        }

        void Release(Image img)
        {
            img.gameObject.SetActive(false);
            pool.Push(img);
        }

        Image NewImage(Sprite sprite)
        {
            var go = new GameObject("Touch", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        void Place(Image img, Vector2 screenPoint, float size)
        {
            float scale = GetComponent<Canvas>().scaleFactor;
            img.rectTransform.anchoredPosition = screenPoint / scale;
            img.rectTransform.sizeDelta = Vector2.one * size;
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // A white disc or ring with a soft edge. The image color gives the tint.
        static Sprite MakeSprite(bool filled)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float c = (n - 1) / 2f, outer = n / 2f - 1, width = n * 0.07f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(outer - d);
                if (!filled) a *= Mathf.Clamp01(d - (outer - width));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }

    [ActionName("ui.show_touches")]
    public sealed record ShowTouches(bool On) : IAction<bool>;

    public sealed class ShowTouchesHandler : ActionHandler<ShowTouches, bool>
    {
        readonly TouchOverlayHook overlay;
        public ShowTouchesHandler(TouchOverlayHook overlay) => this.overlay = overlay;

        protected override Task<bool> Handle(ShowTouches a, ActionContext ctx)
        {
            overlay.Enabled = a.On;
            return Task.FromResult(overlay.Enabled);
        }
    }

    static class OverlayCommands
    {
        [CliGroupCommand("ui", "Show or hide the touch overlay: a burst at each press and a trail for each drag")]
        internal static Task<ExitCode> ShowTouches(CommandContext cmd,
            [CliArg("on", "true = show the touches")] bool on = true)
            => Actions.Run(cmd, new VSA.TestSuite.ShowTouches(on));
    }
}
