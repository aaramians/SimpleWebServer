using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebServer
{

    public class Service
    {
        public Service()
        {

        }

        internal virtual void Respond(Request request)
        {
            OnResponding(request);
        }

        internal virtual void OnResponding(Request request)
        {

        }

        internal virtual bool CanRespond(Request request)
        {
            return true;
        }
    }

    public class RouteAuthenticatorService : Service
    {
        private Service authorized;
        private Service unauthorized;

        Dictionary<string, List<string>> routelist;

        public RouteAuthenticatorService(Service authorized, Service unauhtorized)
        {
            this.authorized = authorized;
            this.unauthorized = unauhtorized;
            routelist = new Dictionary<string, List<string>>();

            routelist.Add("unauthenticated", new List<string>());
        }

        public RouteAuthenticatorService AddRoute(string group, string route)
        {
            if (routelist.Keys.Contains(group))
            {
                routelist[group].Add(route);
            }
            else
            {
                var r = new List<string>();
                r.Add(route);
                routelist.Add(group, r);
            }

            return this;
        }

        public RouteAuthenticatorService NewGroup(string sourcegroup, string newgroup)
        {
            var r = new List<string>();
            r.AddRange(routelist[sourcegroup]);
            routelist.Add(newgroup, r);

            return this;
        }

        internal override void OnResponding(Request request)
        {
            base.OnResponding(request);

            if (request.Form != null && request.Form.Keys.Contains("email") && request.Form.Keys.Contains("password"))
            {
                if (request.Session.MySession != null)
                    request.Session.MySession.group = "authenticated";

            }



            // TODO implementation for later OAuths/SSO's for later; complicated auth cases 
            request.Authorization = new AuthorizationComponent(request);
            request.Authorization.Authorized = false;

            if (request.Session.MySession != null)
                if (routelist.Keys.Contains(request.Session.MySession.group))
                    if (routelist[request.Session.MySession.group].Contains(request.Route))
                        request.Authorization.Authorized = true;

            System.Diagnostics.Debug.WriteLine("\t\t[Authen] {0} {1}", request.Route, request.Authorization.Authorized);

            if (request.Authorization.Authorized)
                this.authorized.Respond(request);
            else
                this.unauthorized.Respond(request);
        }
    }

    public class RouterService : Service
    {
        public bool ExitWhenMatch { get; set; } = true;

        List<Service> servicelist;

        public RouterService()
        {
            this.servicelist = new List<Service>();
        }

        public RouterService(Service service) : this()
        {
            this.servicelist = new List<Service>();
            this.servicelist.Add(service);
        }

        public RouterService(List<Service> servicelist) : this()
        {
            this.servicelist.AddRange(servicelist);
        }

        public RouterService(params Service[] service) : this()
        {
            this.servicelist.AddRange(service);
        }

        internal override void OnResponding(Request request)
        {
            base.OnResponding(request);

            foreach (var ep in servicelist)
            {
                if (ep.CanRespond(request))
                {
                    ep.Respond(request);

                    if (ExitWhenMatch)
                        break;
                }
            }
        }
    }

    public class StaticService : Service
    {
        private string content;
        private string mimetype;

        public StaticService(string content, string mimetype) : base()
        {
            this.content = content;
            this.mimetype = mimetype;
        }

        internal override void OnResponding(Request request)
        {
            base.OnResponding(request);

            request.Response = new ResponseComponent(request);
            request.Response.Content = Encoding.ASCII.GetBytes(content);
            request.Response.ContentLength = request.Response.Content.Length;
            request.Response.ContentMimeType = Codes.MimeTypes[mimetype]; ;

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[200]));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {request.Response.ContentLength}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Type: {request.Response.ContentMimeType}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
            request.Socket.Send(request.Response.Content);
        }
    }

    public class HTMLCodeService : Service
    {
        readonly int ResponseCode;
        List<string> headers = new List<string>();

        public HTMLCodeService(int ResponseCode) : base()
        {
            this.ResponseCode = ResponseCode;
        }

        public HTMLCodeService(int ResponseCode, string header) : this(ResponseCode)
        {
            headers.Add(header);
        }

        internal override void OnResponding(Request request)
        {
            base.OnResponding(request);

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[ResponseCode]));
            foreach (var header in headers)
                request.Socket.Send(Encoding.ASCII.GetBytes($"{header}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("Connection: Closed\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes(string.Empty));
        }
    }

    public class SingleUserAccessService
    {

    }

    public class FileService : Service
    {

        protected class FileCache
        {
            public string ContentRoute { get; set; }
            public long ContentLength { get; set; }
            public long StreamPosition { get; set; }
            public byte[] Content { get; set; }
            public string ContentMimeType { get; internal set; }
        }
        public enum CachingPolicy
        {
            OnAccess, PerAccess, Nothing
        }

        private string rootpath;
        private string virtualpath;
        protected Dictionary<string, FileCache> filecachelist;
        internal CachingPolicy cache { get; set; } = CachingPolicy.OnAccess;

        public FileService(string rootpath, string virtualpath)
        {
            this.rootpath = rootpath;
            this.virtualpath = virtualpath;
            filecachelist = new Dictionary<string, FileCache>();
        }

        internal override bool CanRespond(Request request)
        {
            bool retval = base.CanRespond(request);

            string file = $"{this.rootpath}{request.Route}";

            retval &= System.IO.File.Exists(file);

            return retval;
        }

        internal override void OnResponding(Request request)
        {
            if (request.Response == null)
                request.Response = new ResponseComponent(request);

            base.OnResponding(request);

            foreach (string mime in Codes.MimeTypes.Keys)
                if (request.Route.EndsWith(mime))
                    request.Response.ContentMimeType = Codes.MimeTypes[mime];

            FileCache cache = null;

            if (filecachelist.Keys.Contains(request.Route))
                cache = filecachelist[request.Route];

            bool partial = false;
            int seek = 0;
            long? fsPosition = null;
            long? fsLength = null;

            if (cache == null || cache.Content == null)
            {
                string file = $"{this.rootpath}{this.virtualpath}{request.Route}";

                if (request.RangeStart.HasValue)
                    seek = request.RangeStart.Value;

                using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    // TODO check for accept ranges in request header, otherwise streamout the entire content
                    request.Response.Content = new byte[4 * 1024 * 1024];

                    if (seek > 0)
                        fs.Seek(seek, System.IO.SeekOrigin.Begin);

                    request.Response.ContentLength = fs.Read(request.Response.Content, 0, request.Response.Content.Length);

                    if (fs.Position < fs.Length || seek > 0)
                        partial = true;

                    // TODO not to store the entire buffer
                    if (!partial)
                    {
                        byte[] t = new byte[request.Response.ContentLength];
                        Buffer.BlockCopy(request.Response.Content, 0, t, 0, (int)request.Response.ContentLength);

                        filecachelist.Add(this.virtualpath + request.Route, cache = new FileCache()
                        {
                            ContentLength = request.Response.ContentLength,
                            Content = t,
                            ContentRoute = request.Route,
                            ContentMimeType = request.Response.ContentMimeType
                        });

                        Debug.WriteLine("\t\t[Servic] cached miss with {0} bytes", cache.ContentLength, null);
                    }

                    fsPosition = fs.Position;
                    fsLength = fs.Length;
                }
            }
            else if (cache != null && cache.Content != null)
            {
                Debug.WriteLine("\t\t[servic] cached hit with {0} bytes", cache.ContentLength, null);

                request.Response.Content = cache.Content;
                request.Response.ContentLength = cache.ContentLength;
                request.Response.ContentMimeType = cache.ContentMimeType;

            }

            if (partial)
                request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[206]));
            else
                request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[200]));

            if (partial)
                request.Socket.Send(Encoding.ASCII.GetBytes("Accept-Ranges: bytes\r\n"));

            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {request.Response.ContentLength}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Type: {request.Response.ContentMimeType}\r\n"));

            foreach (var header in request.Session.AdditionalHeaders)
                request.Socket.Send(Encoding.ASCII.GetBytes($"{header}\r\n"));

            if (partial)
                request.Socket.Send(Encoding.ASCII.GetBytes(string.Format("Content-Range: bytes {0}-{1}/{2}\r\n", seek, fsPosition.Value - 1, fsLength.Value)));

            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
            request.Socket.Send(request.Response.Content, (int)request.Response.ContentLength, System.Net.Sockets.SocketFlags.None);
        }
    }

    public class FilePackService : FileService
    {
        string filepack;

        public FilePackService(string filepack) : base(null, null)
        {
            this.filepack = filepack;

        }
        public void Pack(string rootpath, string virtualroot)
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(filepack, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write))
            {
                byte[] bytes;
                var files = System.IO.Directory.EnumerateFiles(rootpath, "*.*", System.IO.SearchOption.AllDirectories);

                var headers = new List<string>();

                foreach (var file in files)
                {
                    if (virtualroot != null)
                        headers.Add(virtualroot + file.Substring(rootpath.Length));
                    else
                        headers.Add(file);

                    headers.Add(fs.Position.ToString());

                    bytes = System.IO.File.ReadAllBytes(file);
                    fs.Write(bytes, 0, bytes.Length);

                    headers.Add(bytes.Length.ToString());
                }

                string headerstart = fs.Position.ToString();

                // a  random guid used for read verification
                bytes = Encoding.ASCII.GetBytes("7B341AAF5A7046AF9C8072BB083E343E\r\n");
                fs.Write(bytes, 0, bytes.Length);

                // a checksum of files

                // file count
                bytes = Encoding.ASCII.GetBytes(files.Count().ToString() + "\r\n");
                fs.Write(bytes, 0, bytes.Length);

                // total files count
                foreach (var header in headers)
                {
                    bytes = Encoding.ASCII.GetBytes(header + "\r\n");
                    fs.Write(bytes, 0, bytes.Length);
                }

                headerstart = headerstart.ToString().PadLeft(10, '0');
                headerstart = headerstart.Substring(headerstart.Length - 10, 10);

                bytes = Encoding.ASCII.GetBytes(headerstart);
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        public void Unpack()
        {
            if (filecachelist == null)
            {
                filecachelist = new Dictionary<string, FileCache>();

                using (System.IO.FileStream fs = new System.IO.FileStream(filepack, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    // reading header - start posision/getting to file listing
                    byte[] bytes = new byte[10];
                    fs.Seek(-10, System.IO.SeekOrigin.End);
                    fs.Read(bytes, 0, 10);
                    string headerstart = Encoding.ASCII.GetString(bytes);
                    fs.Seek(long.Parse(headerstart), System.IO.SeekOrigin.Begin);

                    var sr = new System.IO.StreamReader(fs);

                    // extracting magic string for verification
                    var magicstring = sr.ReadLine();
                    if (magicstring != "7B341AAF5A7046AF9C8072BB083E343E")
                        throw new InvalidOperationException("magic string miss match!");

                    // extracting file count
                    var filecount = int.Parse(sr.ReadLine());

                    // creating file list
                    for (int i = 0; i < filecount; i++)
                    {
                        var path = sr.ReadLine();
                        var position = sr.ReadLine();
                        var length = sr.ReadLine();

                        filecachelist.Add(path, new FileCache() { ContentRoute = path, StreamPosition = long.Parse(position), ContentLength = long.Parse(length) });
                    }

                    // extracting magic string for verification


                    // are we supposed to cache
                    if (cache == CachingPolicy.OnAccess)
                        foreach (var f in filecachelist.Values)
                        {
                            f.Content = new byte[f.ContentLength];

                            if (f.StreamPosition != fs.Position)
                                fs.Seek(f.StreamPosition, System.IO.SeekOrigin.Begin);

                            fs.Read(f.Content, 0, (int)f.ContentLength);

                            filecachelist.Add(f.ContentRoute, f);
                        }
                }
            }


        }

        public void Read(string filename)
        {
            if (filecachelist == null)
                Unpack();

            if (filecachelist.Keys.Contains(filename))
            {
                var file = filecachelist[filename];

                if (file.Content == null)
                    using (System.IO.FileStream fs = new System.IO.FileStream(filepack, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        var bytes = new byte[(int)file.ContentLength];

                        fs.Seek(file.StreamPosition, System.IO.SeekOrigin.Begin);
                        fs.Read(bytes, 0, (int)file.ContentLength);

                        if (cache == CachingPolicy.PerAccess)
                            file.Content = bytes;
                    }
                else
                {

                }
            }
            else
            {
                // file not found
            }
        }
    }

    public class RouteService : Service
    {
        readonly string Route;

        public RouteService(string route)
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


    public class XMLHttpRequestService : RouteService
    {
        // TODO what is preventing multiple runs MultiThread > Single Thread queue
        internal Func<ResponseComponent> Service;

        public XMLHttpRequestService(string route) : base(route)
        {
            //, Func<ResponseComponent> service
            //this.Service = service;
        }

        internal override void OnResponding(Request request)
        {
            try
            {
                request.Response = Service();
                // TODO timeout maybe?
            }
            catch (Exception)
            {
                throw;
            }

            base.OnResponding(request);

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[200]));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {request.Response.Content.Length}\r\n"));
            if (string.IsNullOrEmpty(request.Response.ContentMimeType))
                request.Socket.Send(Encoding.ASCII.GetBytes($"Content-Type: {request.Response.ContentMimeType}\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("\r\n"));
            request.Socket.Send(request.Response.Content);
        }

    }

    // https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events
    public class ServerSentEventService : RouteService
    {
        public ServerSentEventService(string route) : base(route)
        {

        }

        internal override void OnResponding(Request request)
        {
            base.OnResponding(request);

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

    public class WebSocketService : RouteService
    {
        public Action<byte[]> Receive;

        Request LastRequest;

        public WebSocketService(string route) : base(route) { }

        internal override void OnResponding(Request request)
        {
            base.OnResponding(request);

            var sha1 = new System.Security.Cryptography.SHA1Managed();

            var token = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes($"{request.SecWebSocketKey}258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

            request.Socket.Send(Encoding.ASCII.GetBytes(Codes.HttpReposnes[101]));
            request.Socket.Send(Encoding.ASCII.GetBytes("Upgrade: websocket\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes("Connection: Upgrade\r\n"));
            request.Socket.Send(Encoding.ASCII.GetBytes($"Sec-WebSocket-Accept: {token}\r\n"));
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
