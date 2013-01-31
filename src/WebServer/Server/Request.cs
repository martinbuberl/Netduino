using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using Netduino.WebServer.Core.Enums;
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
        public Uri Url;
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
                    SendResponse();
                }

                _connection.Close();
            }
        }

        public void ParseRequestMessage(string requestMessage)
        {
            // normalize the line endings
            requestMessage = requestMessage.Replace("\r\n", "\r");

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
                        // TODO, MB, 1/30/2013: Implement missing HTTP methods. Don't forget to update the 405 Allow params
                        //case "PUT":
                        //    HttpMethod = HttpMethod.Put;
                        //    break;
                        //case "POST":
                        //    HttpMethod = HttpMethod.Post;
                        //    break;
                        //case "DELETE":
                        //    HttpMethod = HttpMethod.Delete;
                        //    break;
                        default:
                            HttpMethod = HttpMethod.Unknown;
                            break;
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
                        Url = new Uri(
                            String.Concat("http://", headerValue, RawUrl).ToLower(), UriKind.Absolute
                        );

                        break;
                }

                Headers[headerName] = headerValue;
            }
        }

        public void SendResponse()
        {
            byte[] responseBody;
            byte[] responseMessage;

            if (HttpMethod == HttpMethod.Unknown)
            {
                responseBody = GetResponseBody(405);
                responseMessage = GetResponseMessage(405, responseBody);
            }
            else
            {
                // check if it was a REST API request
                if (Url.Segments().Length > 1 && (Url.Segments()[1].ToLower() == "api" || Url.Segments()[1].ToLower() == "api/"))
                {

                }

                responseBody = GetResponseBody(200);
                responseMessage = GetResponseMessage(200, responseBody);
            }

            // combine byte arrays
            byte[] response = new byte[responseMessage.Length + responseBody.Length];
            Array.Copy(responseMessage, 0, response, 0, responseMessage.Length);
            Array.Copy(responseBody, 0, response, responseMessage.Length, responseBody.Length);

            _connection.Send(response);
        }

        private static byte[] GetResponseBody(int statusCode)
        {
            string title = String.Empty;
            string body = String.Empty;

            switch (statusCode)
            {
                case 200:
                    title = "HTTP Status 200";
                    body = "OK";
                    break;
                case 405:
                    title = "HTTP Error 405";
                    body = "Method not allowed";
                    break;
            }

            string responseBody = String.Concat(
                "<!DOCTYPE html>",
                "<html>",
                "<head>",
                "<meta charset=\"utf-8\">",
                "<title>", title, "</title>",
                "</head>",
                "<body>", body, "</body>",
                "</html>"
                );

            return Encoding.UTF8.GetBytes(responseBody);
        }

        private static byte[] GetResponseMessage(int statusCode, byte[] responseBody)
        {
            string responseMessage = String.Empty;

            switch (statusCode)
            {
                case 200:
                    responseMessage = String.Concat(
                        "HTTP/1.1 200 OK\r\n",
                        "Content-Type: text/html; charset=utf-8\r\n",
                        "Content-Length: ", responseBody.Length, "\r\n",
                        "Date: ", DateTime.Now.ToUniversalTime().ToString("R"), "\r\n\r\n" // RFC1123
                        );
                    break;
                case 405:
                    responseMessage = String.Concat(
                        "HTTP/1.1 405 Method Not Allowed\r\n",
                        "Allow: GET\r\n\r\n",
                        "Content-Type: text/html; charset=utf-8\r\n",
                        "Content-Length: ", responseBody.Length, "\r\n",
                        "Date: ", DateTime.Now.ToUniversalTime().ToString("R"), "\r\n\r\n" // RFC1123
                        );
                    break;
            }

            return Encoding.UTF8.GetBytes(responseMessage);
        }
    }
}
