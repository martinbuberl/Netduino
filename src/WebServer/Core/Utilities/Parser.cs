using System;
using System.Globalization;
using Netduino.WebServer.Core.Enums;
using Netduino.WebServer.Core.Extensions;

namespace Netduino.WebServer.Core.Utilities
{
    public static class Parser
    {
        public const int MaxDoubleDigits = 16;

        public static bool TryParseUInt(string value, out uint result, NumeralSystem numeralSystem = NumeralSystem.Decimal)
        {
            bool sign;
            ulong tmp;

            bool bresult = TryParseUInt64Core(value, numeralSystem == NumeralSystem.Hexadecimal, out tmp, out sign);
            result = (UInt32)tmp;

            return bresult && !sign;
        }

        public static bool TryParseULong(string value, out ulong result)
        {
            bool sign;
            return TryParseUInt64Core(value, false, out result, out sign) && !sign;
        }

        public static bool TryParseLong(string value, out long result)
        {
            result = 0;
            ulong r;
            bool sign;

            if (TryParseUInt64Core(value, false, out r, out sign))
            {
                if (!sign)
                {
                    if (r <= 9223372036854775807)
                    {
                        result = unchecked((long)r);

                        return true;
                    }
                }
                else
                {
                    if (r <= 9223372036854775808)
                    {
                        result = unchecked(-((long)r));

                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryParseDouble(string value, out double result)
        {
            ulong decValue = 0;
            bool hasExpSign = false;
            bool isExpNeg = false;
            int expDigits = 0;
            ulong expValue = 0;
            result = 0;

            if (value == null)
                throw new ArgumentNullException("value");

            int end = value.Length - 1;
            int start = 0;

            // skip whitespaces
            SkipWhiteSpace(value, ref start, ref end);

            // check for leading sign
            bool hasSign = false;
            bool isNeg = false;

            if (start <= end)
                CheckSign(value, ref start, end, ref hasSign, ref isNeg);

            // now parse the real number
            int intDigits = 0;
            ulong intValue = ParseNumberCore(value, ref start, end, MaxDoubleDigits, ref intDigits, true);

            int decDigits = 0;
            if (start <= end)
            {
                // now check for the decimal point and the decimalplaces
                if (CheckSeparator(value, CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator, ref start, end))
                {
                    if (start <= end)
                        decValue = ParseNumberCore(value, ref start, end, MaxDoubleDigits - intDigits, ref decDigits, false);
                }
            }

            // now check for the exponent
            if (start <= end)
            {
                char curChar = value[start];

                if (curChar == 'E' || curChar == 'e')
                {
                    start++;

                    if (start <= end)
                    {
                        // check for sign
                        CheckSign(value, ref start, end, ref hasExpSign, ref isExpNeg);
                        // get exponent
                        if (start <= end)
                            expValue = ParseNumberCore(value, ref start, end, 5, ref expDigits, false);

                        if (expDigits <= 0)
                            return false;
                    }
                }
            }

            if (start <= end) // characters left 
                return false;

            // now calculate the value
            result = intValue;

            if (intDigits > MaxDoubleDigits)
                result *= Math.Pow(10, intDigits - MaxDoubleDigits);

            if (decDigits > 0)
                result += (decValue * Math.Pow(10d, -decDigits));

            if (isNeg)
                result *= -1;

            //now the exponent
            if (expDigits > 0)
            {
                if (isExpNeg)
                {
                    result *= Math.Pow(10d, (double)expValue * -1);
                }
                else
                {
                    // ReSharper disable RedundantCast
                    result *= Math.Pow(10d, (double)expValue);
                    // ReSharper restore RedundantCast
                }
            }
            return true;
        }

        public static long ParseToLong(string value, NumeralSystem numeralSystem = NumeralSystem.Decimal)
        {
            if (numeralSystem == NumeralSystem.Hexadecimal)
            {
                bool sign;
                ulong resultHex;

                if (TryParseUInt64Core(value, true, out resultHex, out sign))
                    return (long)resultHex;
            }
            else
            {
                long resultLong;

                if (TryParseLong(value, out resultLong))
                    return resultLong;
            }

            throw new Exception();
        }

        public static ulong ParseToULong(string value, NumeralSystem numeralSystem = NumeralSystem.Decimal)
        {
            if (numeralSystem == NumeralSystem.Hexadecimal)
            {
                bool sign;
                ulong resultHex;

                if (TryParseUInt64Core(value, true, out resultHex, out sign))
                    return resultHex;
            }
            else
            {
                ulong resultULong;

                if (TryParseULong(value, out resultULong))
                    return resultULong;
            }

            throw new Exception();
        }

        private static bool TryParseUInt64Core(string value, bool parseHex, out ulong result, out bool sign)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            // If number contains the Hex '0x' prefix, then make sure we're
            // managing a Hex number, and skip over the '0x'
            if (value.Length >= 2 && value.Substring(0, 2).ToLower() == "0x")
            {
                value = value.Substring(2);
                parseHex = true;
            }

            char ch;
            bool noOverflow = true;
            result = 0;

            // Skip leading white space.
            int len = value.Length;
            int posn = 0;

            while (posn < len && value[posn].IsWhiteSpace())
            {
                posn++;
            }

            // Check for leading sign information.
            NumberFormatInfo nfi = CultureInfo.CurrentUICulture.NumberFormat;
            string posSign = nfi.PositiveSign;
            string negSign = nfi.NegativeSign;
            sign = false;

            while (posn < len)
            {
                ch = value[posn];
                if (!parseHex && ch == negSign[0])
                {
                    sign = true;
                    ++posn;
                }
                else if (!parseHex && ch == posSign[0])
                {
                    sign = false;
                    ++posn;
                }
                //else if (ch == thousandsSep[0])
                //{
                //    ++posn;
                //}
                else if ((parseHex && ((ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f'))) ||
                         (ch >= '0' && ch <= '9'))
                {
                    break;
                }
                else
                {
                    return false;
                }
            }

            // Bail out if the string is empty.
            if (posn >= len)
                return false;

            // Parse the main part of the number.
            uint low = 0;
            uint high = 0;
            uint digit;
            ulong tempa, tempb;

            if (parseHex)
            {
                #region Parse a hexadecimal value

                do
                {
                    // Get the next digit from the string.
                    ch = value[posn];
                    if (ch >= '0' && ch <= '9')
                    {
                        digit = (uint)(ch - '0');
                    }
                    else if (ch >= 'A' && ch <= 'F')
                    {
                        digit = (uint)(ch - 'A' + 10);
                    }
                    else if (ch >= 'a' && ch <= 'f')
                    {
                        digit = (uint)(ch - 'a' + 10);
                    }
                    else
                    {
                        break;
                    }

                    // Combine the digit with the result, and check for overflow.
                    if (noOverflow)
                    {
                        // ReSharper disable RedundantCast
                        tempa = ((ulong)low) * ((ulong)16);
                        tempb = ((ulong)high) * ((ulong)16);
                        tempb += (tempa >> 32);
                        // ReSharper restore RedundantCast

                        if (tempb > 0xFFFFFFFF)
                        {
                            // Overflow has occurred.
                            noOverflow = false;
                        }
                        else
                        {
                            tempa = (tempa & 0xFFFFFFFF) + digit;
                            tempb += (tempa >> 32);

                            if (tempb > 0xFFFFFFFF)
                            {
                                // Overflow has occurred.
                                noOverflow = false;
                            }
                            else
                            {
                                low = unchecked((uint)tempa);
                                high = unchecked((uint)tempb);
                            }
                        }
                    }

                    ++posn; // Advance to the next character.
                } while (posn < len);

                #endregion
            }
            else
            {
                #region Parse a decimal value

                do
                {
                    // Get the next digit from the string.
                    ch = value[posn];
                    if (ch >= '0' && ch <= '9')
                    {
                        digit = (uint)(ch - '0');
                    }
                    //else if (ch == thousandsSep[0])
                    //{
                    //    Ignore thousands separators in the string.
                    //    ++posn;
                    //    continue;
                    //}
                    else
                    {
                        break;
                    }

                    // Combine the digit with the result, and check for overflow.
                    if (noOverflow)
                    {
                        // ReSharper disable RedundantCast
                        tempa = ((ulong)low) * ((ulong)10);
                        tempb = ((ulong)high) * ((ulong)10);
                        // ReSharper restore RedundantCast
                        tempb += (tempa >> 32);

                        if (tempb > 0xFFFFFFFF)
                        {
                            // Overflow has occurred.
                            noOverflow = false;
                        }
                        else
                        {
                            tempa = (tempa & 0xFFFFFFFF) + digit;
                            tempb += (tempa >> 32);

                            if (tempb > 0xFFFFFFFF)
                            {
                                // Overflow has occurred.
                                noOverflow = false;
                            }
                            else
                            {
                                low = unchecked((uint)tempa);
                                high = unchecked((uint)tempb);
                            }
                        }
                    }

                    ++posn;// Advance to the next character.
                } while (posn < len);

                #endregion
            }

            // Process trailing white space.
            if (posn < len)
            {
                do
                {
                    ch = value[posn];

                    if (ch.IsWhiteSpace())
                    {
                        ++posn;
                    }
                    else
                    {
                        break;
                    }
                } while (posn < len);

                if (posn < len)
                    return false;
            }

            // Return the results to the caller.
            // ReSharper disable RedundantCast
            result = (((ulong)high) << 32) | ((ulong)low);
            // ReSharper restore RedundantCast
            return noOverflow;
        }

        private static bool CheckSeparator(string value, string sep, ref int start, int end)
        {
            int strLength = sep.Length;

            if (strLength > 0)
            {
                char curChar = value[start];
                char strChar = sep[0];

                // check for first Character at the beginning
                if (curChar == strChar)
                {
                    int counter = 1;
                    int current = start + 1;

                    while (counter < strLength && current <= end)
                    {
                        if (value[current] != sep[counter])
                            break;

                        current++;
                        counter++;
                    }
                    if (counter >= strLength) // string found
                    {
                        // so update to new start position
                        start = current;

                        return true;
                    }
                }
            }

            return false;
        }

        private static void CheckSign(string value, ref int start, int end, ref bool hasSign, ref bool isNeg)
        {
            int counter;
            int current;
            string sign = CultureInfo.CurrentUICulture.NumberFormat.NegativeSign;
            char signChar = sign[0];
            int signLength = sign.Length;
            char curChar = value[start];

            // check for negative sign at the beginning
            if (curChar == signChar)
            {
                counter = 1;
                current = start + 1;

                if (signLength > 1)
                {
                    while (counter < signLength && current <= end)
                    {
                        if (value[current] != sign[counter])
                            break;

                        current++;
                        counter++;
                    }
                }

                if (counter >= signLength)
                {
                    hasSign = true;
                    isNeg = true;
                    start = current;

                    return;
                }
            }

            // check for positive sign at the beginning
            sign = CultureInfo.CurrentUICulture.NumberFormat.PositiveSign;
            signChar = sign[0];
            signLength = sign.Length;

            if (curChar == signChar)
            {
                counter = 1;
                current = start + 1;

                if (signLength > 1)
                {
                    while (counter < signLength && current <= end)
                    {
                        if (value[current] != sign[counter])
                            break;

                        current++;
                        counter++;
                    }
                }

                if (counter >= signLength)
                {
                    hasSign = true;
                    start = current;
                }
            }
        }

        private static void SkipWhiteSpace(string value, ref int start, ref int end)
        {
            while (start <= end && value[start].IsWhiteSpace())
            {
                start++;
            }

            // remove trailing whitespaces
            if (start <= end)
            {
                while (start <= end && value[end].IsWhiteSpace())
                {
                    end--;
                }
            }
        }

        /// <summary>
        /// Parse the number beginning at str[start] up to maximal str[end].
        /// </summary>
        /// <param name="value">Character array containing the data to parse.</param>
        /// <param name="start">Index of the first character to parse.</param>
        /// <param name="end">Index of the last character to parse.</param>
        /// <param name="maxDigits">Must not extend 18 else an overflow may occure.</param>
        /// <param name="numDigits">Is updated to the number of significant digits parsed.</param>
        /// <param name="allowGroupSep"></param>
        private static ulong ParseNumberCore(string value, ref int start, int end, int maxDigits, ref int numDigits, bool allowGroupSep)
        {
            ulong ulwork = 0;
            string sep = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator; // now parse the real number

            while (start <= end)
            {
                char curChar = value[start];

                if (curChar >= '0' && curChar <= '9')
                {
                    if (numDigits < maxDigits)
                        ulwork = ulwork * 10 + unchecked((uint)(curChar - '0'));

                    start++;
                    numDigits++;
                }
                else
                {
                    // check for groupseparator if allowed
                    if (!allowGroupSep || !CheckSeparator(value, sep, ref start, end))
                        break;
                }
            }

            return ulwork;
        }
    }
}
