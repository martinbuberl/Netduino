using System;

namespace Netduino.WebServer.Core.Enums
{
    [Flags]
    public enum HttpMethod
    {
        Unknown = 0,

        /// <see href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html" />
        Get = 1 << 0,
        Put = 1 << 1,
        Post = 1 << 2,
        Delete = 1 << 3,
        Head = 1 << 4
    }
}
