using System;
using System.Collections;

namespace Netduino.WebServer.Core.Extensions
{
    public static class UriExtensions
    {
        public static string[] Segments(this Uri uri)
        {
            string[] segments;
            string path = uri.AbsolutePath;

            if (path.Length == 0)
            {
                segments = new string[0];
            }
            else
            {
                // clean querystring
                if (path.Contains('?'))
                    path = path.Substring(0, path.IndexOf('?'));

                ArrayList pathSegments = new ArrayList();
                int current = 0;

                while (current < path.Length)
                {
                    int next = path.IndexOf('/', current);

                    if (next == -1)
                        next = path.Length - 1;

                    pathSegments.Add(path.Substring(current, (next - current) + 1));
                    current = next + 1;
                }

                segments = (string[])(pathSegments.ToArray(typeof(string)));
            }

            return segments;
        }

        public static string Query(this Uri uri)
        {
            string path = uri.AbsolutePath;

            if (!path.Contains('?'))
                return String.Empty;

            int queryStringStart = path.IndexOf('?');

            return path.Substring(queryStringStart, path.Length - queryStringStart);
        }

        public static Hashtable QueryString(this Uri uri)
        {
            Hashtable queryStrings = new Hashtable();
            string query = uri.Query().Replace("?", String.Empty);
            string[] keyValues = query.Split('&');

            for (int i = 0; i < keyValues.Length; i++)
            {
                string[] key = keyValues[i].Split('=');

                if (key.Length > 1) {
                    queryStrings.Add(key[0], key[1]);
                }
            }

            return queryStrings;
        }
    }
}
