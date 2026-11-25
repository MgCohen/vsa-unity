using System.Diagnostics;
using UnityEngine;

namespace StatusLab
{
    // Game code that blocks the main thread in Update, one time, for Seconds. Forces a hang inside Play Mode.
    public class PlayHang : MonoBehaviour
    {
        public int Seconds = 90;
        bool done;

        void Update()
        {
            if (done) return;
            done = true;
            UnityEngine.Debug.Log("STATUSLAB_PLAY_HANG_START");
            var watch = Stopwatch.StartNew();
            long count = 0;
            while (watch.Elapsed.TotalSeconds < Seconds) count++;
            UnityEngine.Debug.Log("STATUSLAB_PLAY_HANG_END " + count);
        }
    }
}
