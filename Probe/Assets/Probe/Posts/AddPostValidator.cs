using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

namespace Probe.Posts {
    // A pipeline step that no code names. Validator<T> has [AutoRegister], so the scan finds it.
    sealed class AddPostValidator : Validator<AddPostRequest> {
        public override Error Validate(IEnvelope<AddPostRequest> envelope) =>
            string.IsNullOrEmpty(envelope.Message.Title) ? new Error("posts.title", "A post needs a title.") : null;
    }
}
