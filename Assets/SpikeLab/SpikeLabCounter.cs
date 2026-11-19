using UnityEngine;
using UnityEngine.UI;

namespace SpikeLab
{
    [RequireComponent(typeof(Button))]
    public sealed class SpikeLabCounter : MonoBehaviour
    {
        public static int Clicks { get; private set; }

        void Awake()
        {
            Clicks = 0;
            GetComponent<Button>().onClick.AddListener(() => Clicks++);
        }
    }
}
