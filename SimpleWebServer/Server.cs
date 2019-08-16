using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebServer
{
    internal class ClientConnection
    {
        public TcpClient tcpsck;
        private ServerListener serverListener;

        public ClientConnection(ServerListener serverListener)
        {
            this.serverListener = serverListener;
        }

        List<string> parseContentDisposition(string line)
        {
            var split = new List<string>();

            int j = 0;
            bool quoting = false;
            for (int i = j; i < line.Length; i++)
            {
                if (line[i] == '"')
                    if (!quoting)
                        quoting = true;
                    else
                        quoting = false;

                if (line[i] == ' ' || line[i] == '\r')
                    if (!quoting)
                    {
                        if (line[i - 1] == ';')
                            split.Add(line.Substring(j, i - j - 1));
                        else
                            split.Add(line.Substring(j, i - j));

                        j = i + 1;
                    }
            }
            return split;
        }

        string URIParse(string input)
        {
            char[] chinput = input.ToCharArray();
            int j = 0;
            for (int i = 0; i < input.Length; i++)
            {

                if (chinput[i] == '%')
                {
                    string d1 = new string(new char[] { chinput[i], chinput[i + 1], chinput[i + 2] });

                    if (ASCIIURLEncoding.UTF8Encoding.Keys.Contains(d1))
                    {
                        char m = ASCIIURLEncoding.UTF8Encoding[d1][0];
                        chinput[j] = m;
                        i = i + 2;
                        j = j + 1;
                    }
                    else
                    {
                        if (chinput[i + 3] == '%')
                        {
                            string d2 = d1 + new string(new char[] { chinput[i + 3], chinput[i + 4], chinput[i + 5] });
                            if (ASCIIURLEncoding.UTF8Encoding.Keys.Contains(d2))
                            {
                                char m = ASCIIURLEncoding.UTF8Encoding[d2][0];
                                chinput[j] = m;
                                i = i + 5;
                                j = j + 1;
                            }
                            else
                            {
                                if (chinput[i + 6] == '%')
                                {
                                    string d3 = d2 + new string(new char[] { chinput[i + 6], chinput[i + 7], chinput[i + 8] });

                                    if (ASCIIURLEncoding.UTF8Encoding.Keys.Contains(d3))
                                    {
                                        char m = ASCIIURLEncoding.UTF8Encoding[d3][0];
                                        chinput[j] = m;
                                        i = i + 8;
                                        j = j + 1;
                                    }
                                }

                            }

                        }
                    }

                }
                else
                {
                    chinput[j] = chinput[i];
                    j = j + 1;
                }
            }


            return new string(chinput, 0, j);
        }



        public void Handle(object p1)
        {
            tcpsck = p1 as TcpClient;

            Trace.TraceInformation("Connected {0} - {1}", tcpsck.Client.RemoteEndPoint, tcpsck.Client.RemoteEndPoint.AddressFamily);

            try
            {
                var request = new Request(this);

                // TODO implement a timeout
                // wait for connection
                while (!tcpsck.Connected)
                    Debug.WriteLine("waiting for connection");

                tcpsck.ReceiveTimeout = 30 * 1000;

                //while (tcpsck.Available < 10)
                //    Debug.WriteLine("waiting for request");

                var buffer = new Byte[4 * 1024];
                int bufferLength = 0;

                // Data buffer
                bufferLength = tcpsck.Client.Receive(buffer);

                Debug.WriteLine("\t[Client] receive {0} bytes", bufferLength, null);

                if (bufferLength < 1)
                {
                    Debug.WriteLine("\t[Client] nothing to respond", bufferLength, null);
                    return;
                }
                // TODO initial receive might not be enough, so far no issues, I think browsers make sure to send the headers in single stream write

                string boundaryMarker = null;

                // initial headers extractions
                int start = 0;
                for (int i = 0; i < bufferLength; i++)
                {
                    // looking for eol
                    if (buffer[i] == '\r' && buffer[i + 1] == '\n')
                    {
                        var line = Encoding.ASCII.GetString(buffer, start, i - start);

                        start = i + 2;

                        if (line == "")
                            break;

                        Debug.WriteLine("\t[Header] {0}", line, null);

                        request.RawLines.Add(line);

                        // processing GET
                        if (line.StartsWith("GET") || line.StartsWith("POST"))
                        {
                            // TODO longer url encoded requests passing buffer length

                            var t = line.Split(' ');
                            request.Command = t[0];

                            if (t.Length > 0)
                            {
                                int j = t[1].IndexOf('?');

                                if (j > -1)
                                {
                                    request.Route = t[1].Substring(0, j);
                                    request.QueryString = t[1].Substring(j + 1);


                                    request.Form = request.QueryString
                                        .Split('&')
                                        .Select(q => new { Position = q.IndexOf('='), Line = q })
                                        .ToDictionary(
                                            w => w.Line.Substring(0, w.Position),
                                            w => URIParse(w.Line.Substring(w.Position + 1)));

                                }
                                else
                                {
                                    request.Route = t[1];
                                }
                            }

                            if (t.Length > 1)
                                request.Version = t[2];
                        }

                        // processing headers - Content-Length
                        if (line.StartsWith("Content-Length:"))
                        {
                            var t = line.Split(' ')[1];
                            request.ContentLength = int.Parse(t);
                        }

                        if (line.StartsWith("Range: bytes="))
                        {
                            // ex> Range: bytes=1048576-
                            var t = line.Substring("Range: bytes=".Length).Split('-');

                            request.RangeStart = int.Parse(t[0]);

                            if (t[1] != string.Empty)
                                request.RageEnd = int.Parse(t[1]);
                        }

                        // processing headers - Content-Type
                        if (line.StartsWith("Content-Type:"))
                        {
                            var t = line.Split(' ');
                            request.ContentType = t[1];
                            if (t.Length > 2)
                                if (t[2].StartsWith("boundary="))
                                    boundaryMarker = "--" + t[2].Substring("boundary=".Length);
                        }

                        if (line.StartsWith("Sec-WebSocket-Key:"))
                        {
                            request.SecWebSocketKey = line.Split(' ')[1];
                        }

                        if (line.StartsWith("Cookie:"))
                        {
                            request.Cookie = new CookieComponent(request, line);
                        }
                    }
                }

                // processing content
                if (request.ContentType == "application/x-www-form-urlencoded")
                {
                    request.Content = new byte[request.ContentLength];
                    System.Buffer.BlockCopy(buffer, start, request.Content, 0, bufferLength - start);
                    var cStart = bufferLength - start;

                    while (cStart < request.ContentLength)
                    {
                        bufferLength = tcpsck.Client.Receive(buffer);
                        System.Buffer.BlockCopy(buffer, 0, request.Content, cStart, bufferLength);
                        cStart += bufferLength;
                    }

                    request.Form = Encoding.ASCII.GetString(request.Content)
                        .Split('&')
                        .Select(q => new { Position = q.IndexOf('='), Line = q })
                        .ToDictionary(
                            w => w.Line.Substring(0, w.Position),
                            w => URIParse(w.Line.Substring(w.Position + 1)));
                }

                if (request.ContentType == "application/json;")
                {
                    byte[] t = new byte[request.ContentLength];
                    System.Buffer.BlockCopy(buffer, start, t, 0, bufferLength - start);
                    var cStart = bufferLength - start;


                    while (cStart < request.ContentLength)
                    {
                        bufferLength = tcpsck.Client.Receive(buffer);

                        System.Buffer.BlockCopy(buffer, 0, t, cStart, bufferLength);
                        cStart += bufferLength;
                    }
                }

                if (request.ContentType == "multipart/form-data;")
                {
                    // caching
                    byte[] t = new byte[request.ContentLength];
                    System.Buffer.BlockCopy(buffer, start, t, 0, bufferLength - start);
                    var cStart = bufferLength - start;

                    while (cStart < request.ContentLength)
                    {
                        bufferLength = tcpsck.Client.Receive(buffer);
                        System.Buffer.BlockCopy(buffer, 0, t, cStart, bufferLength);
                        cStart += bufferLength;
                    }

                    cStart = 0;
                    for (int i = cStart; i < t.Length; i++)
                    {
                        string line = null;
                        if (t[i] == '\r' && t[i + 1] == '\n')
                        {
                            line = Encoding.ASCII.GetString(t, cStart, i - cStart);
                            cStart = i + 2;
                        }
                        else
                        {
                            continue;
                        }

                        // termination sign
                        if (line.EndsWith(boundaryMarker + "--"))
                            break;

                        if (!line.EndsWith(boundaryMarker))
                            throw new InvalidOperationException("Expecting boundary");

                        Debug.WriteLine("\t[Content] boundary: {0}", line, null);


                        var bLines = new List<string>();

                        // extract headers
                        for (int ib = cStart; ib < t.Length; ib++)
                        {
                            if (t[ib] == '\r' && t[ib + 1] == '\n')
                            {
                                var bline = Encoding.ASCII.GetString(t, cStart, ib - cStart);

                                cStart = ib + 2;

                                if (bline == "")
                                    break;

                                Debug.WriteLine("\t\t[Header] {0}", bline, null);

                                bLines.Add(bline);
                            }
                        }

                        // extract content
                        for (int ic = cStart; ic < t.Length; ic++)
                        {
                            bool match = true;
                            int j = 0;

                            // match case with boundary
                            while (match && j < boundaryMarker.Length)
                                if (t[ic + j] == boundaryMarker[0 + j])
                                    ++j;
                                else
                                    match = false;

                            if (match)
                            {
                                var payload = new byte[ic - cStart - 2];
                                System.Buffer.BlockCopy(t, cStart, payload, 0, ic - cStart - 2);

                                // + 2 for cr and lf
                                Debug.WriteLine("\t\t[Content] length: {0}", payload.Length, null);
                                Debug.WriteLineIf(payload.Length < 256, string.Format("\t\t[Content] {0}", Encoding.ASCII.GetString(t, cStart, ic - cStart - 2)));
                                cStart = i = ic;
                                break;
                            }
                        }
                    }
                }


                request.Session = new SessionComponent(request);

                serverListener.Endpoint.Respond(request);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                    Trace.TraceError("Timeout - {0}", ex.Message);
#if (DEBUG)
                //else
                //    throw ex;
#endif
            }

            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
#if (DEBUG)
                throw ex;
#endif
            }
            finally
            {
                tcpsck.Close();
            }
        }

    }

    class ServerListener
    {
        private IPAddress address;
        private int port;

        public SimpleWebServer.Service Endpoint { get; set; }

        public ServerListener(IPAddress address, int port, Service endpoint)
        {
            this.address = address;
            this.port = port;
            this.Endpoint = endpoint;
        }

        public void Listen()
        {
            var ep = new IPEndPoint(address, port);
            var listener = new TcpListener(ep);
            try
            {

                // listening on network interface
                listener.Start(10);

                // queue up to 10 requests
                Trace.TraceInformation("Listening http://{0}:{1}/Index.html", "localhost", ep.Port);

                while (true)
                {
                    var client = new ClientConnection(this);
                    var t = new Thread(new ParameterizedThreadStart(client.Handle));
                    t.IsBackground = true;
                    t.Start(listener.AcceptTcpClient());
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                throw e;
            }
        }
    }

    public class ServerThread
    {
        readonly ServerListener server;
        public WebSocketService test;
        public ServerThread()
        {
            var staticEp = new StaticService("<!DOCTYPE html><html><head></head><body><p>Authorization Failed!</p></body></html>", ".html");
            var wsEP = new WebSocketService("/TestWS");
            var sseEP = new ServerSentEventService("/TestSSE");
            var fileEP = new FileService("./../../../wwwroot", "");
            var ajaxEP = new XMLHttpRequestService("/TestAjax");// () => { return new ResponseComponent("OK"); });
            var HTML404Endpoint = new HTMLCodeService(404);
            var HTML302Endpoint = new HTMLCodeService(302, "Location: /login.html");

            var routerEp = new RouterService(ajaxEP, sseEP, wsEP, fileEP, HTML404Endpoint);

            var AuthenticatorEP = new RouteAuthenticatorService(routerEp, HTML302Endpoint);
            AuthenticatorEP
                .AddRoute("unauthenticated", "/login.html")
                .AddRoute("unauthenticated", "/favicon.ico")
                .NewGroup("unauthenticated", "authenticated")
                .AddRoute("authenticated", "/index.html")
                .AddRoute("authenticated", "/content/main.js")
                .AddRoute("authenticated", "/content/video.mp4");
           

            server = new ServerListener(IPAddress.Loopback, 65125, AuthenticatorEP);

            test = wsEP;
            wsEP.Receive = (e) =>
            {
                Debug.WriteLine(Encoding.ASCII.GetString(e));
            };
        }

        public void RegisteredEndpoint(EndPoint p)
        {

        }

        public void Start()
        {
            var t = new Thread(new ThreadStart(server.Listen));
            t.Start();
        }
    }

}
