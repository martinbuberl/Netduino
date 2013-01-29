using System;
using Microsoft.SPOT;
using Netduino.WebServer.Core.Extensions;

namespace Netduino.WebServer.Core.Utilities
{
    public static class Converter
    {
        /// <summary>
        /// Converts a specified code point into a Unicode encoded string.
        /// </summary>
        public static string FromUtf32(int utf32)
        {
            if (utf32 < 0 || utf32 > 1114111 || utf32 >= 55296 && utf32 <= 57343)
                throw new ArgumentOutOfRangeException("utf32");

            if (utf32 < 65536)
                return ((char)utf32).ToString();

            utf32 -= 65536;

            return new string(new[] { (char)(utf32 / 1024 + 55296), (char)(utf32 % 1024 + 56320) });
        }

        public static string FromUtf32(uint utf32)
        {
            return FromUtf32((int)utf32);
        }

        /// <summary>
        /// Converts a hexadecimal number to a hexadecimal string.
        /// </summary>
        public static string ToHexString(int value)
        {
            byte[] bytes = Reflection.Serialize(value, typeof(int));

            return new string(ToHexStringArray(bytes));
        }

        /// <summary>
        /// Converts a hexadecimal number to a hexadecimal string.
        /// </summary>
        public static string ToHexString(double value)
        {
            byte[] bytes = Reflection.Serialize(value, typeof(double));

            return new string(ToHexStringArray(bytes));
        }

        private static char[] ToHexStringArray(byte[] bytes)
        {
            char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            char[] chars = new char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];

                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }

            return chars;
        }

        /// <summary>
        /// Creates a Guid from a string value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Guid ToGuid(string value)
        {
            byte[] b = new byte[16];

            b[0] = TwoCharsToByte(value[6], value[7]);
            b[1] = TwoCharsToByte(value[4], value[5]);
            b[2] = TwoCharsToByte(value[2], value[3]);
            b[3] = TwoCharsToByte(value[0], value[1]);
            // -
            b[4] = TwoCharsToByte(value[11], value[12]);
            b[5] = TwoCharsToByte(value[9], value[10]);
            // -
            b[6] = TwoCharsToByte(value[16], value[17]);
            b[7] = TwoCharsToByte(value[14], value[15]);
            // -
            b[8] = TwoCharsToByte(value[19], value[20]);
            b[9] = TwoCharsToByte(value[21], value[22]);
            // -
            b[10] = TwoCharsToByte(value[24], value[25]);
            b[11] = TwoCharsToByte(value[26], value[27]);
            b[12] = TwoCharsToByte(value[28], value[29]);
            b[13] = TwoCharsToByte(value[30], value[31]);
            b[14] = TwoCharsToByte(value[32], value[33]);
            b[15] = TwoCharsToByte(value[34], value[35]);

            return new Guid(b);
        }
        
        /// <summary>
        /// Ccombines two characters into a single byte value.
        /// </summary>
        private static byte TwoCharsToByte(char charOne, char charTwo)
        {
            byte byteOne;
            byte byteTwo;

            if ((charOne - 0x57) < 0)
            {
                byteOne = (byte)(charOne - 0x30);
            }
            else
            {
                byteOne = (byte)(charOne - 0x57);
            }

            if ((charTwo - 0x57) < 0)
            {
                byteTwo = (byte)(charTwo - 0x30);
            }
            else
            {
                byteTwo = (byte)(charTwo - 0x57);
            }

            return (byte)((byteOne << 4) | byteTwo);
        }

        /// <summary>
        /// Converts an ISO 8601 (UTC) time/date format string into a DateTime object.
        /// </summary>
        public static DateTime FromIso8601(string dateTime)
        {
            // Check to see if format contains the timezone ID, or contains UTC reference
            // Neither means it's localtime
            bool utc = dateTime.EndsWith("Z");

            string[] parts = dateTime.Split(new[] { 'T', 'Z', ':', '-', '.', '+' });

            // We now have the time string to parse, and we'll adjust
            // to UTC or timezone after parsing
            string year = parts[0];
            string month = (parts.Length > 1) ? parts[1] : "1";
            string day = (parts.Length > 2) ? parts[2] : "1";
            string hour = (parts.Length > 3) ? parts[3] : "0";
            string minute = (parts.Length > 4) ? parts[4] : "0";
            string second = (parts.Length > 5) ? parts[5] : "0";
            string ms = (parts.Length > 6) ? parts[6] : "0";

            DateTime dt = new DateTime(Convert.ToInt32(year), Convert.ToInt32(month), Convert.ToInt32(day), Convert.ToInt32(hour), Convert.ToInt32(minute), Convert.ToInt32(second), Convert.ToInt32(ms));

            // If a time offset was specified instead of the UTC marker,
            // add/subtract in the hours/minutes
            if ((utc == false) && (parts.Length >= 9))
            {
                // There better be a timezone offset
                string hourOffset = (parts.Length > 7) ? parts[7] : "";
                string minuteOffset = (parts.Length > 8) ? parts[8] : "";
                if (dateTime.Contains("+"))
                {
                    dt = dt.AddHours(Convert.ToDouble(hourOffset));
                    dt = dt.AddMinutes(Convert.ToDouble(minuteOffset));
                }
                else
                {
                    dt = dt.AddHours(-(Convert.ToDouble(hourOffset)));
                    dt = dt.AddMinutes(-(Convert.ToDouble(minuteOffset)));
                }
            }

            if (utc)
            {
                // Convert the Kind to DateTimeKind.Utc if string Z present
                dt = new DateTime(0, DateTimeKind.Utc).AddTicks(dt.Ticks);
            }

            return dt;
        }

        /// <summary>
        /// Converts a DateTime object into an ISO 8601 (UTC) string.
        /// </summary>
        public static string ToIso8601(DateTime dateTime)
        {
            string result = dateTime.Year.ToString() + "-" +
                            TwoDigits(dateTime.Month) + "-" +
                            TwoDigits(dateTime.Day) + "T" +
                            TwoDigits(dateTime.Hour) + ":" +
                            TwoDigits(dateTime.Minute) + ":" +
                            TwoDigits(dateTime.Second) + "." +
                            ThreeDigits(dateTime.Millisecond) + "Z";

            return result;
        }

        /// <summary>
        /// Converts an ASP.NET AJAX JSON string into a DateTime object.
        /// </summary>
        public static DateTime FromAspNetAjax(string dateTime)
        {
            string[] parts = dateTime.Split(new[] { '(', ')' });

            long ticks = Convert.ToInt64(parts[1]);

            // Create a Utc DateTime based on the tick count
            DateTime dt = new DateTime(ticks, DateTimeKind.Utc);

            return dt;
        }

        /// <summary>
        /// Ensures a two-digit number with leading zero if necessary.
        /// </summary>
        private static string TwoDigits(int value)
        {
            if (value < 10)
                return "0" + value.ToString();

            return value.ToString();
        }

        /// <summary>
        /// Ensures a three-digit number with leading zeros if necessary.
        /// </summary>
        private static string ThreeDigits(int value)
        {
            if (value < 10)
                return "00" + value.ToString();

            if (value < 100)
                return "0" + value.ToString();

            return value.ToString();
        }

        /// <summary>
        /// Converts a Double to a string using a variant of the Double.ToString() method.
        /// The problem with the built-in ToString() method is that it wont automatically
        /// size the precision to fit the number.  So a number like 33.3 gets truncated to
        /// just 33 unless you specify exactly the right amount of precision.  This method
        /// attempts to determine the right amount of precision.
        /// </summary>
        /// <remarks>CAUTION!!! I've seen many times when the built-in ToString() method
        /// returns a rounding error when you specify any meaningful precision.</remarks>
        public static string ToString(double value)
        {
            // First, convert to string with impossibly-long precision
            string fullValue = value.ToString("F64");

            // Find the last non-zero number in the mantissa
            int mantissaLen = 0;
            int i = fullValue.Length - 1;

            for (; i >= 0; i--)
            {
                if (fullValue[i] == '.')
                {
                    // we made it all the way to the dot
                    break;
                }

                // Stop counting the mantissa once you reach the last non-zero value in the number
                if (fullValue[i] != '0' && (mantissaLen == 0))
                {
                    mantissaLen = i;
                }
            }

            // If the entire mantissa was zero, then add 2 for the dot and one mantissa digit
            mantissaLen = (mantissaLen > 0) ? mantissaLen : 2;

            // Truncate the trailing zeros in the mantissa
            fullValue = fullValue.Substring(0, mantissaLen + 1);

            return fullValue;
        }
    }
}
