using VSA.Unity;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// The host component of the spike. Put it on one GameObject of the first scene: Awake runs SpikeApp.
    /// The CLI commands work only after that.
    /// </summary>
    public sealed class SpikeHost : VsaHost<SpikeApp>
    {
    }
}
