using System.Threading;

namespace Netduino.WebServer
{
    public class Program
    {
        public static void Main()
        {
            WebServer webServer = new WebServer();

            //using (var streamReader = new StreamReader(@"\SD\test.html"))
            //{
            //    String line = String.Empty;
            //    while ((line = streamReader.ReadLine()) != null)
            //    {
            //        Debug.Print(line);
            //    }
            //}


            //        string request = new string(Encoding.UTF8.GetChars(buffer));

            //        if (request.IndexOf("ON") >= 0)
            //            _led.Write(true);
            //        if (request.IndexOf("OFF") >= 0)
            //            _led.Write(false);

            //        string statusText = "LED id " + (_led.Read() ? "ON" : "OFF") + ".";

            //        string response = "HTTP/1.1 200 OK\r\n" +
            //                          "Content-Type: text/html; charset=utf-8\r\n\r\n" +
            //                          "<html><head><title>Foo</title></head><body>" + statusText + "</body><html>";

            //        clientSocket.Send(Encoding.UTF8.GetBytes(response));
            //    }

            //    
            //}

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
