using UnityEngine;

namespace TestSuiteLab
{
    public sealed class LabSceneMarker : MonoBehaviour
    {
        void Awake() => Debug.Log($"[Lab] {gameObject.scene.name} Awake frame={Time.frameCount}");

        void Start() => Debug.Log($"[Lab] {gameObject.scene.name} Start frame={Time.frameCount}");

        void OnDestroy() => Debug.Log($"[Lab] {gameObject.scene.name} OnDestroy frame={Time.frameCount}");
    }
}
