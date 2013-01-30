using System;
using System.Net;
using System.Net.Sockets;
using Netduino.WebServer.Core.Abstraction;

namespace Netduino.WebServer.Core.Utilities
{
    public static class SyncTime
    {
        /// <summary>
        /// Synchronize Netduino's local time with a time server (NTP).
        /// </summary>
        /// <param name="timeServer">Time server (NTP) address e.g. 'time.nist.gov'.</param>
        /// <param name="timeZoneOffset">Time zone difference in hours from UTC.</param>
        /// <see href="http://weblogs.asp.net/mschwarz/archive/2008/03/09/wrong-datetime-on-net-micro-framework-devices.aspx" />
        /// <see href="http://www.jaypm.com/2011/09/setting-the-netduinos-datetime-automatically/" />
        public static bool Update(string timeServer, int timeZoneOffset)
        {
            DebugWrapper.Print("Synchronize local time with NTP:");
            DebugWrapper.Print(String.Empty);
            DebugWrapper.Print("   NTP Address . . . . . . . . . . . : " + timeServer);
            DebugWrapper.Print("   Time Zone Offset. . . . . . . . . : " + timeZoneOffset);

            DebugWrapper.Print(String.Empty);

            try
            {
                DateTime currentTime = GetNtpTime(timeServer, timeZoneOffset);
                Microsoft.SPOT.Hardware.Utility.SetLocalTime(currentTime);

                DebugWrapper.Print("Synchronization successfull:");
                DebugWrapper.Print(String.Empty);
                DebugWrapper.Print("   Local Time. . . . . . . . . . . . : " + DateTime.Now.ToUniversalTime().ToString("R"));
                DebugWrapper.Print(String.Empty);

                return true;
            }
            catch
            {
                DebugWrapper.Print("Synchronization failed:");
                DebugWrapper.Print(String.Empty);
                DebugWrapper.Print("   Local Time. . . . . . . . . . . . : " + DateTime.Now.ToUniversalTime().ToString("R"));
                DebugWrapper.Print(String.Empty);

                return false;
            }
        }

        private static DateTime GetNtpTime(String timeServer, int timeZoneOffset)
        {
            // find endpoint for time server
            IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(timeServer).AddressList[0], 123);

            // make send/receive buffer
            byte[] ntpData = new byte[48];

            // connect to time server
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                // set 10s send/receive timeout and connect
                socket.SendTimeout = socket.ReceiveTimeout = 10000; // 10,000 ms
                socket.Connect(ep);

                // set protocol version
                ntpData[0] = 0x1B;

                // send request
                socket.Send(ntpData);

                // receive time
                socket.Receive(ntpData);

                socket.Close();
            }

            const byte offsetTransmitTime = 40;

            ulong intpart = 0;
            ulong fractpart = 0;

            for (int i = 0; i <= 3; i++)
            {
                intpart = (intpart << 8) | ntpData[offsetTransmitTime + i];
            }

            for (int i = 4; i <= 7; i++)
            {
                fractpart = (fractpart << 8) | ntpData[offsetTransmitTime + i];
            }

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            TimeSpan offsetAmount = new TimeSpan(timeZoneOffset, 0, 0);
            DateTime networkDateTime = (dateTime + offsetAmount);

            return networkDateTime;
        }
    }
}
