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
        private readonly Thread _thread;
        private readonly int _port;

        public WebServer(int port = 80)
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces()[0];
            networkInterface.EnableDhcp();

            // not sure if that's necessary yet
            while (networkInterface.IPAddress == "0.0.0.0")
                Thread.Sleep(500);

            DebugHelper.NetworkInterface(networkInterface);

            _port = port;
            _thread = new Thread(Start);
            _thread.Start();

            Debug.Print("Started web server in thread '" + _thread.GetHashCode() + "'.");
        }

        private void Start()
        {
            using (Socket listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                IPEndPoint server = new IPEndPoint(IPAddress.Any, _port);

                listenerSocket.Bind(server);
                listenerSocket.Listen(1);

                while (true)
                {
                    using (Socket connection = listenerSocket.Accept())
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
