using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace VSA.TestSuite.Spike
{
    /// <summary>Endpoint of the find object slice. The CLI and scripts call it.</summary>
    public static class FindObjectEndpoint
    {
        [CliCommand("object.find", "Find an active object by hierarchy path and return its screen point")]
        public static Task<FindResult> Run(
            [CliArg("path", "Hierarchy path, for example LabCanvas/LabButton")] string path)
            => Actions.Run(new FindObject(path));
    }
}
