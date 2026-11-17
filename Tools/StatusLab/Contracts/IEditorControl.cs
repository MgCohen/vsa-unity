namespace StatusLab
{
    /// <summary>The only way to change the Editor. A fake implementation tests the supervisor without Unity.</summary>
    public interface IEditorControl
    {
        Task Start(Target target, CancellationToken cancel);
        Task Quit(Target target, CancellationToken cancel);
        void CloseWindow(Target target);
        void Stop(Target target);
        bool AnswerDialog(Target target, string titlePart, string button);
    }
}
