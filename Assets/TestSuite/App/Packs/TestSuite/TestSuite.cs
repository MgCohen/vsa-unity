using Umpire.Kernels;
using Umpire.Vsa;

namespace VSA.TestSuite.Packs.TestSuite
{
    public class TestSuite<THost> : Vsa<TestSuiteArchitecture, THost>
        where THost : AppHost, new()
    {
    }
}
