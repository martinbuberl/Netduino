using System;

namespace Netduino.WebServer.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string s)
        {
// ReSharper disable ReplaceWithStringIsNullOrEmpty
            return s == null || s == String.Empty;
// ReSharper restore ReplaceWithStringIsNullOrEmpty
        }

        public static bool StartsWith(this string s, string value)
        {
            return s.IndexOf(value) == 0;
        }

        public static bool Contains(this string s, string value)
        {
            return s.IndexOf(value) > 0;
        }

    }
}
