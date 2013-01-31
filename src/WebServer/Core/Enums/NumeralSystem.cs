using System;

namespace Netduino.WebServer.Core.Enums
{
    [Flags]
    public enum NumeralSystem
    {
        Decimal = 1 << 0,
        Hexadecimal = 1 << 1
    }
}
