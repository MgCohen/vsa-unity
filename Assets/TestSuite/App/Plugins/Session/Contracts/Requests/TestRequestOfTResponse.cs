using Umpire.Vsa.Contracts.Requests;

#pragma warning disable UMP010201
namespace VSA.TestSuite.Plugins.Session
{
    public abstract record TestRequest<TResponse> : TestRequest, IRequest<TResponse>
        where TResponse : IResponse;
}
