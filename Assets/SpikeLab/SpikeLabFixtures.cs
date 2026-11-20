using UnityEngine;
using UnityEngine.UI;

namespace SpikeLab
{
    public static class SpikeLabFixtures
    {
        public static T Add<T>(string name, Vector2 position, bool active = true)
            where T : Component
        {
            var canvas = GameObject.Find("LabCanvas").transform;
            var old = canvas.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var part = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(T));
            part.transform.SetParent(canvas, false);
            var rect = (RectTransform)part.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120, 80);
            part.SetActive(active);
            return part.GetComponent<T>();
        }
    }
}
