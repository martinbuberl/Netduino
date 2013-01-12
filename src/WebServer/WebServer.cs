using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using Netduino.WebServer.Utilities;

namespace Netduino.WebServer
{
    public class WebServer
    {
        private readonly int _port;
        private Thread _thread;
        private Socket _listener;

        public WebServer(int port = 80)
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];
            networkInterface.EnableDhcp();

            // not sure if that's necessary yet
            while (networkInterface.IPAddress == "0.0.0.0")
                Thread.Sleep(500);

            DebugHelper.NetworkInterface(networkInterface);

            _port = port;
        }

        public bool IsAlive
        {
            get
            {
                return _thread.IsAlive;
            }
        }

        public void Start()
        {
            _thread = new Thread(Listen);
            _thread.Start();

            Debug.Print("Started web server in thread '" + _thread.GetHashCode() + "'.");
        }

        public void Stop()
        {
            _listener.Close();
            _thread.Abort();

            Debug.Print("Stoped web server. Terminated thread '" + _thread.GetHashCode() + "'.");
        }

        public void Suspend()
        {
            _thread.Suspend();

            Debug.Print("Suspended web server in thread '" + _thread.GetHashCode() + "'.");
        }

        public void Resume()
        {
            _thread.Resume();

            Debug.Print("Resumed web server in thread '" + _thread.GetHashCode() + "'.");
        }

        private void Listen()
        {
            using (_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IPEndPoint server = new IPEndPoint(IPAddress.Any, _port);

                _listener.Bind(server);
                _listener.Listen(1);

                while (true) // !done
                {
                    using (Socket connection = _listener.Accept())
                    {
                        bool dataReady = connection.Poll(-1, SelectMode.SelectRead);

                        if (dataReady && connection.Available > 0)
                        {
                            // create buffer and receive raw bytes
                            byte[] buffer = new byte[connection.Available];
                            int byteRead = connection.Receive(buffer, buffer.Length, SocketFlags.None);

                            // convert to string, will include HTTP headers
                            string request = new String(Encoding.UTF8.GetChars(buffer));
                        }

                        connection.Close();
                    }
                }
            }
        }
    }
}
