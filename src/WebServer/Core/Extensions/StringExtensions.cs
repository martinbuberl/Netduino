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

        /// <summary>
        /// Determines whether a character is whitespace.
        /// </summary>
        public static bool IsWhiteSpace(this char c)
        {
            return c == 32 || c >= 9 && c <= 13 || (c == 160 || c == 133);
        }

        public static bool StartsWith(this string s, string value)
        {
            return s.IndexOf(value) == 0;
        }

        public static bool EndsWith(this string s, string value)
        {
            try
            {
                return (s.Substring(s.Length - value.Length, value.Length) == value);
            }
// ReSharper disable EmptyGeneralCatchClause
            catch
// ReSharper restore EmptyGeneralCatchClause
            {
            }

            return false;
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

        public static string PadLeft(this string s, int count, char pad)
        {
            for (int i = 0; i < count; i++)
            {
                s = pad + s;
            }

            return s;
        }

        public static string PadRight(this string s, int count, char pad)
        {
            for (int i = 0; i < count; i++)
            {
                s = s + pad;
            }

            return s;
        }
    }
}
