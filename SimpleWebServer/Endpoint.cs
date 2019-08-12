using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebServer
{

    public class Endpoint
    {
        public Endpoint()
        {

        }

        internal virtual void OnRespond(Request request)
        {

        }

        internal virtual bool CanRespond(Request request)
        {
            return true;
        }
    }

    public class RoutedEndpoint : Endpoint
    {
        readonly string Route;

        public RoutedEndpoint(string route)
        {
            this.Route = route;
        }

        internal override bool CanRespond(Request request)
        {
            bool retval = base.CanRespond(request);
            retval &= this.Route == request.Route;

            return retval;
        }

    }


    public class RouterEndpoint : Endpoint
    {
        public bool ExitWhenMatch { get; set; } = true;

        List<Endpoint> endpoints;

        public RouterEndpoint()
        {
            this.endpoints = new List<Endpoint>();
        }

        public RouterEndpoint(Endpoint endpoint) : this()
        {
            this.endpoints = new List<Endpoint>();
            this.endpoints.Add(endpoint);
        }

        public RouterEndpoint(List<Endpoint> endpoints) : this()
        {
            this.endpoints.AddRange(endpoints);
        }

        public RouterEndpoint(params Endpoint[] endpoints) : this()
        {
            this.endpoints.AddRange(endpoints);
        }

        internal override void OnRespond(Request request)
        {
            base.OnRespond(request);

            foreach (var ep in endpoints)
            {
                if (ep.CanRespond(request))
                {
                    ep.OnRespond(request);

                    if (ExitWhenMatch)
                        break;
                }
            }
        }
    }

    public class StaticEndpoint : Endpoint
    {
        readonly byte[] content;

        public StaticEndpoint(string content) : base()
        {
            this.content = Encoding.ASCII.GetBytes(content);
        }

        internal override void OnRespond(Request request)
        {
            base.OnRespond(request);

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[200]));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {this.content.Length}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Type: {Codes.MimeTypes}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
            request.Socket.Send(this.content);
        }
    }

    public class HTMLStatusEndpoint : Endpoint
    {
        readonly int ResponseCode;

        public HTMLStatusEndpoint(int ResponseCode) : base()
        {
            this.ResponseCode = ResponseCode;
        }

        internal override void OnRespond(Request request)
        {
            base.OnRespond(request);

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[ResponseCode]));
            request.Socket.Send(Encoding.ASCII.GetBytes("Connection: Closed\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes(string.Empty));
        }
    }

    public class StaticFileEndpoint : Endpoint
    {
        private string path;

        public StaticFileEndpoint(string path)
        {
            this.path = path;
        }

        internal override bool CanRespond(Request request)
        {
            bool retval = base.CanRespond(request);

            string file = $"{this.path}{request.Route}";

            retval &= System.IO.File.Exists(file);

            return retval;
        }

        internal override void OnRespond(Request request)
        {
            base.OnRespond(request);

            if (request.Route != null)
                foreach (string mime in Codes.MimeTypes.Keys)
                    if (request.Route.EndsWith(mime))
                        request.mimetype = Codes.MimeTypes[mime];

            string file = $"{this.path}{request.Route}";

            int seek = 0;
            if (request.ContentRangeStart.HasValue)
                seek = request.ContentRangeStart.Value;

            bool partial = false;

            if (request.mimetype != null)
                using (var stream = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    int size = 0;

                    byte[] buf = new byte[4 * 1024 * 1024];

                    if (seek > 0)
                        stream.Seek(seek, System.IO.SeekOrigin.Begin);

                    size = stream.Read(buf, 0, buf.Length);

                    if (stream.Position < stream.Length || seek > 0)
                        partial = true;

                    if (partial)
                        request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[206]));
                    else
                        request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[200]));

                    if (partial)
                        request.Socket.Send(Encoding.ASCII.GetBytes("Accept-Ranges: bytes\r\n"));

                    request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {size}\r\n"));
                    request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Type: {request.mimetype}\r\n"));

                    if (partial)
                        request.Socket.Send(Encoding.ASCII.GetBytes(string.Format("Content-Range: bytes {0}-{1}/{2}\r\n", seek, stream.Position - 1, stream.Length)));

                    request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
                    request.Socket.Send(buf, size, System.Net.Sockets.SocketFlags.None);
                }
        }
    }

    public class WebServiceEndpoint : RoutedEndpoint
    {
        public class Reponse
        {
            public readonly byte[] content;

            public virtual string ContentMIMEtype { get { return "text/plain"; } }
            public Reponse(string resposne)
            {
                this.content = Encoding.ASCII.GetBytes(resposne);
            }
        }

        public class JSONResponse : Reponse
        {
            // "application/json" //JSON text
            // "application/javascript" // JSONP - application/javascript
            public override string ContentMIMEtype { get { return "application/json"; } }
            public JSONResponse(string resposne) : base(resposne)
            {

            }
        }


        public class XMLResposne : Reponse
        {
            public XMLResposne(string resposne) : base(resposne)
            {

            }
        }

        // TODO what is preventing multiple runs MultiThread > Single Thread queue
        public Func<Reponse> Service;

        public WebServiceEndpoint(string route, Func<Reponse> service) : base(route)
        {
            this.Service = service;
        }

        internal override void OnRespond(Request request)
        {
            base.OnRespond(request);
            Reponse response;

            try
            {
                // TODO timeout maybe?
                response = Service();
            }
            catch (Exception)
            {
                throw;
            }

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[200]));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {response.content.Length}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Type: {response.ContentMIMEtype}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
            request.Socket.Send(response.content);
        }

    }

    // https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events
    public class ServerSentEventEndpoint : RoutedEndpoint
    {
        public ServerSentEventEndpoint(string route) : base(route)
        {

        }

        internal override void OnRespond(Request request)
        {
            base.OnRespond(request);

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[200]));
            request.Socket.Send(Encoding.ASCII.GetBytes("Content-Type: text/event-stream\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("Cache-Control: no-cache\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));

            // TODO frameno

            while (true)
            {
                Send(request, null, null, "Server Side Event - default message", null);
                System.Threading.Thread.Sleep(1000);
                Send(request, null, "customevent", "Server Side Event - customevent", null);
                System.Threading.Thread.Sleep(1000);
            }

        }

        void Send(Request request, int? id, string @event, string data, string retry)
        {
            if (id != null)
                request.Socket.Send(Encoding.ASCII.GetBytes($"id: {id}\r\n"));


            if (@event != null)
                request.Socket.Send(Encoding.ASCII.GetBytes($"event: {@event}\r\n"));

            if (data != null)
                request.Socket.Send(Encoding.ASCII.GetBytes($"data: {data}\r\n"));

            if (retry != null)
                request.Socket.Send(Encoding.ASCII.GetBytes($"retry: {retry}\r\n"));

            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));

        }

    }

    public class WebSocketEndpoint : RoutedEndpoint
    {
        public Action<byte[]> Receive;

        Request LastRequest;

        public WebSocketEndpoint(string route) : base(route)
        {

        }
        internal override void OnRespond(Request request)
        {
            base.OnRespond(request);

            var sha1 = new System.Security.Cryptography.SHA1Managed();
            var SecWebSocketAccept = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes($"{request.SecWebSocketKey}258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[101]));
            request.Socket.Send(Encoding.ASCII.GetBytes("Upgrade: websocket\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("Connection: Upgrade\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Sec-WebSocket-Accept: {SecWebSocketAccept}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));

            LastRequest = request;

            while (true)
                OnReceive(request);
        }

        public void Send(string payload)
        {

            if (LastRequest == null)
                return;
            // TODO temp FIX FIX FIX
            var request = LastRequest;
            var payloadbytes = Encoding.ASCII.GetBytes(payload);

            byte opcode = 0x1;
            byte fin = 1;
            byte b1 = 0;
            byte b2 = 0;

            b1 = (byte)(opcode | fin << 7);

            if (payload.Length < 125)
                b2 = (byte)(b2 | payload.Length);
            else
                throw new NotImplementedException();


            var buff = new byte[payloadbytes.Length + 2];
            buff[0] = b1;
            buff[1] = b2;
            System.Buffer.BlockCopy(payloadbytes, 0, buff, 2, payloadbytes.Length);
            request.Socket.Send(buff);

        }

        void OnReceive(Request request)
        {
            request.Socket.ReceiveTimeout = 0;

            int size = 0;
            var pl = new byte[4 * 1024];

            size = request.Socket.Receive(pl);

            bool fin = (pl[0] & 0x80) != 0;// == 128;
            int opcode = pl[0] & (0xF);

            bool Mask = (pl[1] & 0x80) != 0; //== 128;
            byte plFlag = (byte)(pl[1] & 0x7F);
            int plLen = 0;
            int plStart = 0;
            byte[] plMask = null;

            if (plFlag < 126)
            {
                plLen = plFlag;
                plStart = 2;
                if (Mask)
                {
                    plMask = new byte[4] { pl[2], pl[3], pl[4], pl[5] };
                    plStart = 2 + 4;
                }

            }
            else if (plFlag == 126)
            {
                plLen = (pl[2] << 8 + pl[3]);
                plStart = 4;
                if (Mask)
                {
                    plMask = new byte[4] { pl[4], pl[5], pl[6], pl[7] };
                    plStart = 4 + 4;
                }
            }
            else if (plFlag == 127)
            {
                plLen = (pl[2] << 24 + pl[3] << 16 + pl[4] << 8 + pl[5]);
                plStart = 6;
                if (Mask)
                {
                    plMask = new byte[4] { pl[6], pl[7], pl[8], pl[9] };
                    plStart = 6 + 4;
                }
            }

            // TODO larger payload
            var payload = new byte[plLen];

            int j = 0;
            if (Mask)
                for (int i = plStart; i < plStart + plLen; i++, j++)
                    pl[i] = (byte)(pl[i] ^ plMask[j % 4]);


            Buffer.BlockCopy(pl, plStart, payload, 0, plLen);

            if (Receive != null)
                Receive(payload);

        }

    }



}
