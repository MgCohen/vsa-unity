using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VSA.TestSuite.Spike.Features.Touch
{
    public sealed class TouchOverlayView : MonoBehaviour
    {
        const float DotSize = 32;
        const float BurstSize = 96;
        const float BurstSeconds = 0.4f;
        const float TrailSeconds = 0.5f;
        static readonly Color Tint = new Color(1f, 0.25f, 0.2f, 1f);

        readonly List<Fade> fades = new List<Fade>();
        readonly Stack<Image> pool = new Stack<Image>();
        Sprite disc;
        Sprite ring;
        Image dot;

        public static TouchOverlayView Create()
        {
            var root = new GameObject("VSA.TouchOverlay", typeof(Canvas));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var view = root.AddComponent<TouchOverlayView>();
            view.disc = MakeSprite(true);
            view.ring = MakeSprite(false);
            view.dot = view.NewImage(view.disc);
            view.dot.gameObject.SetActive(false);
            return view;
        }

        public void Press(Vector2 point)
        {
            Add(ring, point, BurstSize * 0.35f, BurstSize, BurstSeconds, 0.9f);
            ShowDot(point);
        }

        public void Move(Vector2 point)
        {
            ShowDot(point);
            Add(disc, point, DotSize * 0.45f, DotSize * 0.2f, TrailSeconds, 0.6f);
        }

        public void Release(Vector2 point)
        {
            dot.gameObject.SetActive(false);
            Add(ring, point, DotSize * 0.9f, DotSize * 1.5f, BurstSeconds * 0.6f, 0.6f);
        }

        public void Clear()
        {
            foreach (var fade in fades) Free(fade.Image);
            fades.Clear();
            dot.gameObject.SetActive(false);
        }

        static Color WithAlpha(float alpha) => new Color(Tint.r, Tint.g, Tint.b, alpha);

        static Sprite MakeSprite(bool filled)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float center = (size - 1) / 2f, outer = size / 2f - 1, width = size * 0.07f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(outer - distance);
                if (!filled) alpha *= Mathf.Clamp01(distance - (outer - width));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        void Update()
        {
            for (var i = fades.Count - 1; i >= 0; i--)
            {
                var fade = fades[i];
                fade.Age += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(fade.Age / fade.Life);
                var ease = 1f - (1f - t) * (1f - t);
                fade.Image.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(fade.StartSize, fade.EndSize, ease);
                fade.Image.color = WithAlpha(fade.StartAlpha * (1f - t));
                if (t < 1f) continue;
                Free(fade.Image);
                fades.RemoveAt(i);
            }
        }

        void ShowDot(Vector2 point)
        {
            Place(dot, point, DotSize);
            dot.color = WithAlpha(0.85f);
            dot.gameObject.SetActive(true);
            dot.transform.SetAsLastSibling();
        }

        void Add(Sprite sprite, Vector2 point, float startSize, float endSize, float life, float alpha)
        {
            var image = pool.Count > 0 ? pool.Pop() : NewImage(sprite);
            image.sprite = sprite;
            image.gameObject.SetActive(true);
            Place(image, point, startSize);
            image.color = WithAlpha(alpha);
            fades.Add(new Fade
            {
                Image = image, Life = life, StartSize = startSize, EndSize = endSize, StartAlpha = alpha,
            });
        }

        void Free(Image image)
        {
            image.gameObject.SetActive(false);
            pool.Push(image);
        }

        Image NewImage(Sprite sprite)
        {
            var part = new GameObject("Touch", typeof(RectTransform), typeof(Image));
            part.transform.SetParent(transform, false);
            var rect = (RectTransform)part.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            var image = part.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        void Place(Image image, Vector2 point, float size)
        {
            image.rectTransform.anchoredPosition = point / GetComponent<Canvas>().scaleFactor;
            image.rectTransform.sizeDelta = Vector2.one * size;
        }

        sealed class Fade
        {
            public Image Image;
            public float Age;
            public float Life;
            public float StartSize;
            public float EndSize;
            public float StartAlpha;
        }
    }
}
