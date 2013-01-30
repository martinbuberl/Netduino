using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT.Net.NetworkInformation;
using Netduino.WebServer.Core.Abstraction;
using Netduino.WebServer.Core.Utilities;

namespace Netduino.WebServer.Server
{
    public class HttpServer
    {
        private readonly int _port;

        private Thread _thread;
        private Socket _listener;
        private bool _cancel;

        public HttpServer(int port = 80)
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];
            networkInterface.EnableDhcp();

            // not sure if that's necessary yet
            while (networkInterface.IPAddress == "0.0.0.0")
                Thread.Sleep(500);

            DebugHelper.NetworkInterface(networkInterface);

            // IPAddress.Parse(networkInterface.IPAddress).GetAddressBytes();
            _port = port;
        }

        public bool IsAlive { get { return _thread.IsAlive; } }

        public void Start()
        {
            _cancel = false;

            _thread = new Thread(Listen);
            _thread.Start();

            DebugWrapper.Print("Started web server. Thread ID: '" + _thread.ManagedThreadId + "'.");
        }

        public void Stop()
        {
            DebugWrapper.Print("Stopping web server. Thread ID: '" + _thread.ManagedThreadId + "'.");

            _listener.Close();
            _thread.Abort();
        }

        private void Listen()
        {
            using (_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IPEndPoint server = new IPEndPoint(IPAddress.Any, _port);

                _listener.Bind(server);
                _listener.Listen(1);

                while (!_cancel)
                {
                    DebugWrapper.Print(".");

                    Request request = new Request(_listener.Accept());
                    Thread thread = new Thread(request.Process);
                    thread.Start();
                    Thread.Sleep(1);
                }
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns
    }
}
