using Microsoft.SPOT;

namespace Netduino.WebServer.Core.Abstraction
{
    public static class DebugWrapper
    {
        public static void Print(string text)
        {
            Debug.Print(text);
        }
    }
}
