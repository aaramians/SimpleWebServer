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
        public string command;
        public string Route;
        public string version;
        public string mimetype = null;
        public List<string> Lines = new List<string>();
        public Request(ClientConnection client)
        {
            this.Socket = client.tcpsck.Client;
        }

        public int ContentLength { get; internal set; }
        public string ContentType { get; internal set; }
        public string queryString { get; internal set; }
        public Socket Socket { get; }
        public int? ContentRangeStart { get; internal set; }
        public int? ContentRangeEnd { get; internal set; }
        public string SecWebSocketKey { get; internal set; }
    }

}
