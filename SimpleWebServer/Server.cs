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
    class Client
    {
        public class Request
        {
            public string command;
            public string route;
            public string version;
            public string mimetype = null;
            public List<string> Lines = new List<string>();

            public int ContentLength { get; internal set; }
            public string ContentType { get; internal set; }
            public string queryString { get; internal set; }
        }

        static Dictionary<int, string> _HttpReposnes;
        static Dictionary<int, string> HttpReposnes
        {
            get
            {
                if (_HttpReposnes == null)
                    _HttpReposnes = new Dictionary<int, string>()
            {
                { 100, "HTTP/1.1 100 Continue\r\n" },
                { 101, "HTTP/1.1 101 Switching Protocols\r\n" },
                { 200, "HTTP/1.1 200 OK\r\n" },
                { 201, "HTTP/1.1 201 Created\r\n" },
                { 202, "HTTP/1.1 202 Accepted\r\n" },
                { 203, "HTTP/1.1 203 Non-Authoritative Information\r\n" },
                { 204, "HTTP/1.1 204 No Content\r\n" },
                { 205, "HTTP/1.1 205 Reset Content\r\n" },
                { 206, "HTTP/1.1 206 Partial Content\r\n" },
                { 300, "HTTP/1.1 300 Multiple Choices\r\n" },
                { 301, "HTTP/1.1 301 Moved Permanently\r\n" },
                { 302, "HTTP/1.1 302 Found\r\n" },
                { 303, "HTTP/1.1 303 See Other\r\n" },
                { 304, "HTTP/1.1 304 Not Modified\r\n" },
                { 305, "HTTP/1.1 305 Use Proxy\r\n" },
                { 307, "HTTP/1.1 307 Temporary Redirect\r\n" },
                { 400, "HTTP/1.1 400 Bad Request\r\n" },
                { 401, "HTTP/1.1 401 Unauthorized\r\n" },
                { 402, "HTTP/1.1 402 Payment Required\r\n" },
                { 403, "HTTP/1.1 403 Forbidden\r\n" },
                { 404, "HTTP/1.1 404 Not Found\r\n" },
                { 405, "HTTP/1.1 405 Method Not Allowed\r\n" },
                { 406, "HTTP/1.1 406 Not Acceptable\r\n" },
                { 407, "HTTP/1.1 407 Proxy Authentication Required\r\n" },
                { 408, "HTTP/1.1 408 Request Timeout\r\n" },
                { 409, "HTTP/1.1 409 Conflict\r\n" },
                { 410, "HTTP/1.1 410 Gone\r\n" },
                { 411, "HTTP/1.1 411 Length Required\r\n" },
                { 412, "HTTP/1.1 412 Precondition Failed\r\n" },
                { 413, "HTTP/1.1 413 Request Entity Too Large\r\n" },
                { 414, "HTTP/1.1 414 Request-URI Too Long\r\n" },
                { 415, "HTTP/1.1 415 Unsupported Media Type\r\n" },
                { 416, "HTTP/1.1 416 Requested Range Not Satisfiable\r\n" },
                { 417, "HTTP/1.1 417 Expectation Failed\r\n" },
                { 500, "HTTP/1.1 500 Internal Server Error\r\n" },
                { 501, "HTTP/1.1 501 Not Implemented\r\n" },
                { 502, "HTTP/1.1 502 Bad Gateway\r\n" },
                { 503, "HTTP/1.1 503 Service Unavailable\r\n" },
                { 504, "HTTP/1.1 504 Gateway Timeout\r\n" },
                { 505, "HTTP/1.1 505 HTTP Version Not Supported\r\n" },
            };

                return _HttpReposnes;
            }
        }

        static Dictionary<string, string> _MimeTypes;
        static Dictionary<string, string> MimeTypes
        {
            get
            {
                if (_MimeTypes == null)
                    _MimeTypes = new Dictionary<string, string>()
            {
               {".rtf", "application/rtf" },
                {".midi", "audio/midi" },
                {".mid", "audio/midi" },
                {".pict", "image/pict" },
                {".pct", "image/pict" },
                {".pic", "image/pict" },
                {".xul", "text/xul" },
                // geenral
                {".js", "application/javascript" },
                {".mjs", "application/javascript" },
                {".json", "application/json" },
                {".doc", "application/msword" },
                {".dot", "application/msword" },
                {".wiz", "application/msword" },
                {".bin", "application/octet-stream" },
                {".a", "application/octet-stream" },
                {".dll", "application/octet-stream" },
                {".exe", "application/octet-stream" },
                {".o", "application/octet-stream" },
                {".obj", "application/octet-stream" },
                {".so", "application/octet-stream" },
                {".oda", "application/oda" },
                {".pdf", "application/pdf" },
                {".p7c", "application/pkcs7-mime" },
                {".ps", "application/postscript" },
                {".ai", "application/postscript" },
                {".eps", "application/postscript" },
                {".m3u", "application/vnd.apple.mpegurl" },
                {".m3u8", "application/vnd.apple.mpegurl" },
                {".xls", "application/vnd.ms-excel" },
                {".xlb", "application/vnd.ms-excel" },
                {".ppt", "application/vnd.ms-powerpoint" },
                {".pot", "application/vnd.ms-powerpoint" },
                {".ppa", "application/vnd.ms-powerpoint" },
                {".pps", "application/vnd.ms-powerpoint" },
                {".pwz", "application/vnd.ms-powerpoint" },
                {".wasm", "application/wasm" },
                {".bcpio", "application/x-bcpio" },
                {".cpio", "application/x-cpio" },
                {".csh", "application/x-csh" },
                {".dvi", "application/x-dvi" },
                {".gtar", "application/x-gtar" },
                {".hdf", "application/x-hdf" },
                {".latex", "application/x-latex" },
                {".mif", "application/x-mif" },
                {".cdf", "application/x-netcdf" },
                {".nc", "application/x-netcdf" },
                {".p12", "application/x-pkcs12" },
                {".pfx", "application/x-pkcs12" },
                {".ram", "application/x-pn-realaudio" },
                {".pyc", "application/x-python-code" },
                {".pyo", "application/x-python-code" },
                {".sh", "application/x-sh" },
                {".shar", "application/x-shar" },
                {".swf", "application/x-shockwave-flash" },
                {".sv4cpio", "application/x-sv4cpio" },
                {".sv4crc", "application/x-sv4crc" },
                {".tar", "application/x-tar" },
                {".tcl", "application/x-tcl" },
                {".tex", "application/x-tex" },
                {".texi", "application/x-texinfo" },
                {".texinfo", "application/x-texinfo" },
                {".roff", "application/x-troff" },
                {".t", "application/x-troff" },
                {".tr", "application/x-troff" },
                {".man", "application/x-troff-man" },
                {".me", "application/x-troff-me" },
                {".ms", "application/x-troff-ms" },
                {".ustar", "application/x-ustar" },
                {".src", "application/x-wais-source" },
                {".xsl", "application/xml" },
                {".rdf", "application/xml" },
                {".wsdl", "application/xml" },
                {".xpdl", "application/xml" },
                {".zip", "application/zip" },
                {".au", "audio/basic" },
                {".snd", "audio/basic" },
                {".mp3", "audio/mpeg" },
                {".mp2", "audio/mpeg" },
                {".aif", "audio/x-aiff" },
                {".aifc", "audio/x-aiff" },
                {".aiff", "audio/x-aiff" },
                {".ra", "audio/x-pn-realaudio" },
                {".wav", "audio/x-wav" },
                {".bmp", "image/x-ms-bmp" },
                {".gif", "image/gif" },
                {".ief", "image/ief" },
                {".jpg", "image/jpeg" },
                {".jpe", "image/jpeg" },
                {".jpeg", "image/jpeg" },
                {".png", "image/png" },
                {".svg", "image/svg+xml" },
                {".tiff", "image/tiff" },
                {".tif", "image/tiff" },
                {".ico", "image/vnd.microsoft.icon" },
                {".ras", "image/x-cmu-raster" },
                {".pnm", "image/x-portable-anymap" },
                {".pbm", "image/x-portable-bitmap" },
                {".pgm", "image/x-portable-graymap" },
                {".ppm", "image/x-portable-pixmap" },
                {".rgb", "image/x-rgb" },
                {".xbm", "image/x-xbitmap" },
                {".xpm", "image/x-xpixmap" },
                {".xwd", "image/x-xwindowdump" },
                {".eml", "message/rfc822" },
                {".mht", "message/rfc822" },
                {".mhtml", "message/rfc822" },
                {".nws", "message/rfc822" },
                {".css", "text/css" },
                {".csv", "text/csv" },
                {".html", "text/html" },
                {".htm", "text/html" },
                {".txt", "text/plain" },
                {".bat", "text/plain" },
                {".c", "text/plain" },
                {".h", "text/plain" },
                {".ksh", "text/plain" },
                {".pl", "text/plain" },
                {".rtx", "text/richtext" },
                {".tsv", "text/tab-separated-values" },
                {".py", "text/x-python" },
                {".etx", "text/x-setext" },
                {".sgm", "text/x-sgml" },
                {".sgml", "text/x-sgml" },
                {".vcf", "text/x-vcard" },
                {".xml", "text/xml" },
                {".mp4", "video/mp4" },
                {".mpeg", "video/mpeg" },
                {".m1v", "video/mpeg" },
                {".mpa", "video/mpeg" },
                {".mpe", "video/mpeg" },
                {".mpg", "video/mpeg" },
                {".mov", "video/quicktime" },
                {".qt", "video/quicktime" },
                {".webm", "video/webm" },
                {".avi", "video/x-msvideo" },
                {".movie", "video/x-sgi-movie" },

            };

                return _MimeTypes;
            }
        }

        public Client()
        {


        }

        public void Handle(object socket)
        {
            var clientSocket = socket as Socket;

            Debug.WriteLine("new connection {0}", clientSocket.RemoteEndPoint);

            try
            {
                Respond(clientSocket);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error! {0}", ex.Message);
                clientSocket.Send(Encoding.ASCII.GetBytes(HttpReposnes[500]));
                clientSocket.Send(Encoding.ASCII.GetBytes("Connection: Closed\r\n"));
                clientSocket.Send(Encoding.ASCII.GetBytes("\r\n"));
#if (DEBUG)
                throw ex;
#endif
            }
            finally
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                clientSocket.Dispose();
            }
        }

        public void Respond(Socket socket)
        {
            var buffer = new Byte[6 * 1024];
            int bufferLength = 0;

            socket.ReceiveTimeout = 30000;
            //socket.SendTimeout = 30000;
            {
                // Data buffer 
                bufferLength = socket.Receive(buffer);

                var request = new Request();

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

                        Debug.WriteLine(line);

                        request.Lines.Add(line);

                        // processing GET
                        if (line.StartsWith("GET") || line.StartsWith("POST"))
                        {

                            // Todo longer url encoded requests passing buffer length

                            var t = line.Split(' ');
                            request.command = t[0];

                            if (t.Length > 0)
                            {
                                int j = t[1].IndexOf('?');

                                if (j > -1)
                                {
                                    request.route = t[1].Substring(0, j);
                                    request.queryString = t[1].Substring(j + 1);
                                }
                                else
                                {
                                    request.route = t[1];
                                }
                            }

                            if (t.Length > 1)
                                request.version = t[2];

                        }

                        // processing headers - Content-Length
                        if (line.StartsWith("Content-Length:"))
                        {
                            var t = line.Split(' ')[1];
                            request.ContentLength = int.Parse(t);
                        }

                        // processing headers - Content-Type
                        if (line.StartsWith("Content-Type:"))
                        {
                            var t = line.Split(' ');
                            request.ContentType = t[1];
                            if (t.Length > 2)
                                if (t[2].StartsWith("boundary="))
                                {
                                    boundaryMarker = "--" + t[2].Substring("boundary=".Length);
                                }

                        }
                    }
                }

                // processing content
                if (request.ContentType == "application/x-www-form-urlencoded")
                {
                    byte[] t = new byte[request.ContentLength];
                    System.Buffer.BlockCopy(buffer, start, t, 0, bufferLength - start);
                    var cStart = bufferLength - start;

                    while (cStart < request.ContentLength)
                    {
                        bufferLength = socket.Receive(buffer);
                        System.Buffer.BlockCopy(buffer, 0, t, cStart, bufferLength);
                        cStart += bufferLength;
                    }
                }

                if (request.ContentType == "application/json;")
                {
                    byte[] t = new byte[request.ContentLength];
                    System.Buffer.BlockCopy(buffer, start, t, 0, bufferLength - start);
                    var cStart = bufferLength - start;


                    while (cStart < request.ContentLength)
                    {
                        bufferLength = socket.Receive(buffer);

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
                        bufferLength = socket.Receive(buffer);
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
                            Debug.WriteLine(line);

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

                        var bLines = new List<string>();

                        // extract headers
                        for (int ib = cStart; ib < t.Length; ib++)
                        {
                            if (t[ib] == '\r' && t[ib + 1] == '\n')
                            {
                                var bline = Encoding.ASCII.GetString(t, cStart, ib - cStart);
                                Debug.WriteLine(bline);

                                cStart = ib + 2;

                                if (bline == "")
                                    break;

                                bLines.Add(bline);
                            }
                        }

                        // extract content
                        for (int ic = cStart; ic < t.Length; ic++)
                        {
                            bool match = true;
                            int j = 0;
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
                                Debug.WriteLineIf(payload.Length < 256, Encoding.ASCII.GetString(t, cStart, ic - cStart - 2));
                                Debug.WriteLineIf(payload.Length >= 256, "Large payload");
                                cStart = i = ic;
                                break;
                            }
                        }
                    }
                }

                //print('{' + str(i) + ', "HTTP/1.1 ' + str(i) + ' ' + __responses[i][0] + '\\r\\n" },')
                if (request.route != null)
                    foreach (string mime in MimeTypes.Keys)
                        if (request.route.EndsWith(mime))
                            request.mimetype = MimeTypes[mime];


                if (request.mimetype != null && System.IO.File.Exists($"wwwroot{request.route}"))
                {
                    byte[] response = System.IO.File.ReadAllBytes($"wwwroot{request.route}");
                    socket.Send(Encoding.ASCII.GetBytes(HttpReposnes[200]));
                    socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {response.Length}\r\n"));
                    socket.Send(Encoding.ASCII.GetBytes($"Content-Type: {request.mimetype}\r\n"));
                    socket.Send(Encoding.ASCII.GetBytes("Connection: Closed\r\n"));
                    socket.Send(Encoding.ASCII.GetBytes("\r\n"));
                    socket.Send(response);

                }
                else
                {
                    socket.Send(Encoding.ASCII.GetBytes(HttpReposnes[404]));
                    socket.Send(Encoding.ASCII.GetBytes("Connection: Closed\r\n"));
                    socket.Send(Encoding.ASCII.GetBytes("\r\n"));
                }

                //data += Encoding.ASCII.GetString(reqBuff, 0, reqBuffLen);

                ////    if (data.IndexOf("<EOF>") > -1)
                ////        break;
                //////}

                //Debug.WriteLine("Text received -> {0} ", data);
                //byte[] message = Encoding.ASCII.GetBytes("Test Server");

                // Send a message to Client  
                // using Send() method 
                //clientSocket.Send(message);

                // Close client Socket using the 
                // Close() method. After closing, 
                // we can use the closed Socket  
                // for a new Client Connection 
            }

        }

    }

    class ServerListener
    {
        public void Listen()
        {
            var ipHost = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddr = ipHost.AddressList[0];
            var localEndPoint = new IPEndPoint(IPAddress.Any, 65125);

            using (var listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {

                    // listening on network interface
                    listener.Bind(localEndPoint);

                    // queue up to 10 requests
                    listener.Listen(10);
                    Debug.WriteLine("Waiting connection ... ");

                    while (true)
                    {
                        var client = new Client();
                        var t = new Thread(new ParameterizedThreadStart(client.Handle));
                        t.Start(listener.Accept());
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);

                    throw e;
                }
            }
        }

    }
    public class Server
    {

        public void Listen()
        {
            var i = new ServerListener();

            var t = new Thread(new ThreadStart(i.Listen));
            t.Start();

            t.Join();
        }
    }

}
