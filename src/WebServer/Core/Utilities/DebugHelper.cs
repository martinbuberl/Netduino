using System;
using Microsoft.SPOT.Net.NetworkInformation;
using Netduino.WebServer.Core.Abstraction;

namespace Netduino.WebServer.Core.Utilities
{
    public static class DebugHelper
    {
        public static void NetworkInterface(NetworkInterface networkInterface)
        {
            DebugWrapper.Print(String.Empty);

            switch (networkInterface.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Ethernet:
                    DebugWrapper.Print("Ethernet adapter:");
                    break;
                case NetworkInterfaceType.Wireless80211:
                    DebugWrapper.Print("Wireless LAN adapter WI-FI:");
                    break;
                default:
                    DebugWrapper.Print("Unknown adapter:");
                    break;
            }

            DebugWrapper.Print(String.Empty);

            DebugWrapper.Print("   Physical Address. . . . . . . . . : " + Format.GetPhysicalAddress(networkInterface.PhysicalAddress));
            DebugWrapper.Print("   DHCP Enabled. . . . . . . . . . . : " + Format.BoolToYesNo(networkInterface.IsDhcpEnabled));
            DebugWrapper.Print("   IPv4 Address. . . . . . . . . . . : " + networkInterface.IPAddress);
            DebugWrapper.Print("   Subnet Mask . . . . . . . . . . . : " + networkInterface.SubnetMask);
            DebugWrapper.Print("   Default Gateway . . . . . . . . . : " + networkInterface.GatewayAddress);

            DebugWrapper.Print(String.Empty);

            DebugWrapper.Print("   DNS Servers . . . . . . . . . . . : " + networkInterface.DnsAddresses[0]);
            DebugWrapper.Print("   Dynamic DNS Enabled . . . . . . . : " + Format.BoolToYesNo(networkInterface.IsDynamicDnsEnabled));

            if (networkInterface.DnsAddresses.Length > 1)
                DebugWrapper.Print("                                       " + networkInterface.DnsAddresses[1]);

            DebugWrapper.Print(String.Empty);
        }
    }
}
