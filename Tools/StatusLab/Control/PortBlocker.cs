using System.Net;
using System.Net.Sockets;

namespace StatusLab
{
    /// <summary>Holds loopback ports open, so a starting Editor cannot take them. Only free ports are taken.</summary>
    public sealed class PortBlocker : IDisposable
    {
        readonly List<TcpListener> listeners = new();

        public IReadOnlyList<int> Ports => listeners.Select(l => ((IPEndPoint)l.LocalEndpoint).Port).ToList();

        public static PortBlocker Take(int first, int last)
        {
            var blocker = new PortBlocker();
            for (var port = first; port <= last; port++)
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    blocker.listeners.Add(listener);
                }
                catch (SocketException)
                {
                    // In use by another process: leave it.
                }
            }
            return blocker;
        }

        public void Dispose()
        {
            foreach (var l in listeners) l.Stop();
            listeners.Clear();
        }
    }
}
