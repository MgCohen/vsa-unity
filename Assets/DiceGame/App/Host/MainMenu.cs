using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceGame.App {
    // The main menu: the start button loads the game scene.
    public sealed class MainMenu : MonoBehaviour {
        public const string GameScene = "DiceGame";

        [SerializeField]
        Button start;

        public Button StartButton {
            get => start;
            set => start = value;
        }

        void Awake() => start.onClick.AddListener(() => SceneManager.LoadScene(GameScene));
    }
}
