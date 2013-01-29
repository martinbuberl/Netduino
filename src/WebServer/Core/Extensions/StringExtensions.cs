using System;
using System.Text;

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

        public static bool Contains(this string s, char c)
        {
            return s.IndexOf(c) > 0;
        }

        public static bool Contains(this string s, string value)
        {
            return s.IndexOf(value) > 0;
        }

        public static string Replace(this string s, char oldChar, char newChar)
        {
            string result = new StringBuilder(s).Replace(oldChar, newChar).ToString();

            return result ?? String.Empty;
        }

        public static string Replace(this string s, string oldValue, string newValue)
        {
            string result = new StringBuilder(s).Replace(oldValue, newValue).ToString();

            return result ?? String.Empty;
        }
    }
}
