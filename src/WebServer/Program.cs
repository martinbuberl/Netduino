using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Netduino.WebServer.Server;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Netduino.WebServer
{
    public class Program
    {
        private static HttpServer _httpServer;

        public static void Main()
        {
            _httpServer = new HttpServer();
            _httpServer.Start();

            // stop/start web server on button push
            InterruptPort button = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);
            button.OnInterrupt += button_OnInterrupt;

            Thread.Sleep(Timeout.Infinite);
        }

        static void button_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            if (_httpServer.IsAlive)
            {
                _httpServer.Stop();
            }
            else
            {
                _httpServer.Start();
            }
        }
    }
}
