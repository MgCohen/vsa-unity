namespace DiceGame.App.Views {
    // One roll of the list: its number and its value.
    public sealed class RollEntry {
        public RollEntry(int number, int value) {
            Number = number;
            Value = value;
        }

        public int Number { get; }

        public int Value { get; }
    }
}
