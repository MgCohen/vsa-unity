using System;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using VSA.TestSuite.Plugins.Session;

namespace VSA.TestSuite.Features.Objects
{
    public static class Objects
    {
        [CliCommand("objects.find", "Find an active object by hierarchy path and return its screen point")]
        public static Task<TraceReply> Find(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
            => SessionBus.Send(new FindObject(path));

        [CliCommand("objects.wait_for", "Wait until an object is in the given state; fail after the time limit")]
        public static Task<TraceReply> WaitFor(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path,
            [CliArg("state", "Active, Inactive or Missing")] string state = "Active",
            [CliArg("seconds", "Time limit in seconds of real time")] float seconds = 5)
            => SessionBus.Send(new WaitForObject(path, Parse(state), seconds));

        [CliCommand("objects.assert_state", "Fail if an object is not in the given state: Active, Inactive or Missing")]
        public static Task<TraceReply> AssertState(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path,
            [CliArg("state", "Active, Inactive or Missing")] string state = "Active")
            => SessionBus.Send(new AssertState(path, Parse(state)));

        [CliCommand("objects.assert_text", "Fail if the text of an object is not the given text")]
        public static Task<TraceReply> AssertText(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton/Label")] string path,
            [CliArg("text", "Expected text")] string text)
            => SessionBus.Send(new AssertText(path, text));

        static ObjectState Parse(string state) => (ObjectState)Enum.Parse(typeof(ObjectState), state, true);
    }
}
