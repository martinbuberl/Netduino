using System;
using System.Collections;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Netduino.WebServer.Core.Extensions;
using Netduino.WebServer.Core.Patterns;

namespace Netduino.WebServer.Server
{
    public class Request : Disposable
    {
        private readonly Socket _connection;

        public Request(Socket connection)
        {
            _connection = connection;
        }

        public HttpMethod HttpMethod;
        public string HttpProtocol;
        public string RawUrl;
        public string Url;
        public Hashtable Headers =  new Hashtable();

        public void Process()
        {
            using (_connection)
            {
                bool dataReady = _connection.Poll(-1, SelectMode.SelectRead);

                if (dataReady && _connection.Available > 0)
                {
                    // create buffer and receive raw bytes
                    byte[] buffer = new byte[_connection.Available];
                    _connection.Receive(buffer, buffer.Length, SocketFlags.None);

                    // convert to string, will include the HTTP request message (request line and headers)
                    string requestMessage = new String(Encoding.UTF8.GetChars(buffer));

                    ParseRequestMessage(requestMessage);
                    HandleRequest();
                }

                _connection.Close();
            }
        }

        public void ParseRequestMessage(string requestMessage)
        {
            // normalize the line endings
            requestMessage = new StringBuilder(requestMessage).Replace("\r\n", "\r").ToString();

            string[] requestMessageLines = requestMessage.Split('\r');

            for (int i = 0; i < requestMessageLines.Length; i++)
            {
                string requestMessageLine = requestMessageLines[i];

                // parse the request line e.g. 'GET /images/logo.png HTTP/1.1'
                if (i == 0)
                {
                    string[] requestLine = requestMessageLine.Split(' ');

                    switch (requestLine[0].ToUpper())
                    {
                        case "GET":
                            HttpMethod = HttpMethod.Get;
                            break;
                        case "POST":
                            HttpMethod = HttpMethod.Post;
                            break;
                        default:
                            throw new NotImplementedException(
                                "The HTTP protocol methods 'DELETE, HEAD, OPTIONS, PUT, TRACE' are not implemented."
                                );
                    }

                    RawUrl = requestLine[1];
                    HttpProtocol = requestLine[2];

                    continue;
                }

                // ignore empty lines
                if (requestMessageLine.IsNullOrEmpty())
                    continue;

                // parse the headers e.g. 'Accept-Language: en'
                int separator = requestMessageLine.IndexOf(':'); 
                string headerName = requestMessageLine.Substring(0, separator);

                int pos = separator + 1;
                while ((pos < requestMessageLine.Length) && (requestMessageLine[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string headerValue = requestMessageLine.Substring(pos, requestMessageLine.Length - pos);

                // parse special header information
                switch (headerName.ToUpper())
                {
                    case "HOST":
                        Url = "http://" + headerValue + RawUrl;
                        break;
                }

                Headers[headerName] = headerValue;
            }
        }

        public void HandleRequest()
        {
            byte[] responseBody = Encoding.UTF8.GetBytes(
                "<html>" +
                "<head>" +
                "</head>" +
                "<body>" +
                "</body>" +
                "</html>"
                );

            byte[] responseMessage = Encoding.UTF8.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html; charset=utf-8\r\n" +
                "Content-Length: " + responseBody.Length + "\r\n" +
                "Date: " + DateTime.Now.ToUniversalTime().ToString("R") + "\r\n\r\n" // RFC1123
                );

            // combine byte arrays
            byte[] response = new byte[responseMessage.Length + responseBody.Length];
            Array.Copy(responseMessage, 0, response, 0, responseMessage.Length);
            Array.Copy(responseBody, 0, response, responseMessage.Length, responseBody.Length);

            _connection.Send(response);
        }
    }

    /// <see href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec9.html" />
    public enum HttpMethod
    {
        /// <summary>Represents an HTTP GET protocol method.</summary>
        Get,
        /// <summary>Represents an HTTP POST protocol method that is used to post a new entity as an addition to a URL.</summary>
        Post
    }
}
