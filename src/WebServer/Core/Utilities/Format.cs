using System;

namespace Netduino.WebServer.Core.Utilities
{
    public static class Format
    {
        /// <summary>
        /// Formats a byte array to a physical address (MAC address).
        /// </summary>
        /// <param name="byteArray">The byte array to format.</param>
        /// <param name="seperator">The seperator.</param>
        /// <returns>The formated phusical address (MAC address).</returns>
        public static string GetPhysicalAddress(byte[] byteArray, char seperator = '-')
        {
            if (byteArray == null)
                return null;

            string physicalAddress = String.Empty;

            for (int i = 0; i < byteArray.Length; i++)
            {
                physicalAddress += byteArray[i].ToString("X2");

                if (i != byteArray.Length - 1)
                    physicalAddress += seperator;
            }

            return physicalAddress;
        }

        /// <summary>
        /// Returns "Yes" if true; otherwise "No".
        /// </summary>
        /// <param name="value">The boolean to format.</param>
        public static string BoolToYesNo(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}
