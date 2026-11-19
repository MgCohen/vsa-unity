using Umpire.Unity;
using Umpire.Vsa;

namespace DiceGame.App {
    // The shell of the dice game. It is on one GameObject of the main menu scene, and Awake starts the app.
    public sealed class DiceShell : Shell<Vsa<DiceHost>> {
    }
}
