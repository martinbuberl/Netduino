using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;

namespace Netduino.WebServer.Core.Utilities
{
    public static class DebugHelper
    {
        public static void NetworkInterface(NetworkInterface networkInterface)
        {
            Debug.Print(String.Empty);

            switch (networkInterface.NetworkInterfaceType)
            {
                case NetworkInterfaceType.Ethernet:
                    Debug.Print("Ethernet adapter:");
                    break;
                case NetworkInterfaceType.Wireless80211:
                    Debug.Print("Wireless LAN adapter WI-FI:");
                    break;
                default:
                    Debug.Print("Unknown adapter:");
                    break;
            }

            Debug.Print(String.Empty);

            Debug.Print("   Physical Address. . . . . . . . . : " + Format.GetPhysicalAddress(networkInterface.PhysicalAddress));
            Debug.Print("   DHCP Enabled. . . . . . . . . . . : " + Format.BoolToYesNo(networkInterface.IsDhcpEnabled));
            Debug.Print("   IPv4 Address. . . . . . . . . . . : " + networkInterface.IPAddress);
            Debug.Print("   Subnet Mask . . . . . . . . . . . : " + networkInterface.SubnetMask);
            Debug.Print("   Default Gateway . . . . . . . . . : " + networkInterface.GatewayAddress);

            Debug.Print(String.Empty);

            Debug.Print("   DNS Servers . . . . . . . . . . . : " + networkInterface.DnsAddresses[0]);
            Debug.Print("   Dynamic DNS Enabled . . . . . . . : " + Format.BoolToYesNo(networkInterface.IsDynamicDnsEnabled));

            if (networkInterface.DnsAddresses.Length > 1)
                Debug.Print("                                       " + networkInterface.DnsAddresses[1]);

            Debug.Print(String.Empty);
        }
    }
}
