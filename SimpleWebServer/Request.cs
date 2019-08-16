using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebServer
{
    internal class Request
    {
        public string Command;
        public string Route;
        public string Version;

        public List<string> RawLines = new List<string>();

        public int ContentLength { get; internal set; }

        public string ContentType { get; internal set; }

        public string QueryString { get; internal set; }
        public Dictionary<string, string> Form { get; internal set; }


        public Socket Socket { get; }

        public ResponseComponent Response { get; set; }

        public AuthorizationComponent Authorization;

        public SessionComponent Session;
        public CookieComponent Cookie;

        public int? RangeStart { get; internal set; }

        public int? RageEnd { get; internal set; }

        public string SecWebSocketKey { get; internal set; }
        public byte[] Content { get; internal set; }

        public Request(ClientConnection client)
        {
            this.Socket = client.tcpsck.Client;
        }
    }

}
