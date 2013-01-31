using System;

namespace Netduino.WebServer.Core.Enums
{
    [Flags]
    public enum DateTimeFormat
    {
        Unknown = 0,

        Iso8601 = 1 << 0,
        Ajax = 1 << 1
    }
}
