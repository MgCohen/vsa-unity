using UnityEngine;
using UnityEngine.UI;

namespace TestSuiteLab
{
    [RequireComponent(typeof(Button))]
    public sealed class LabCounter : MonoBehaviour
    {
        public static int Clicks { get; private set; }

        void Awake()
        {
            Clicks = 0;
            GetComponent<Button>().onClick.AddListener(() => Clicks++);
        }
    }
}
