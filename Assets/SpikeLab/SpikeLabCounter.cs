using UnityEngine;
using UnityEngine.UI;

namespace SpikeLab
{
    /// <summary>Counts the clicks of its Button. The count starts at 0 in each Play Mode session.</summary>
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
