using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebServer
{
    internal class Component
    {
        protected Request request;

        public List<string> AdditionalHeaders = new List<string>();
        public Component(Request request)
        {
            this.request = request;

        }
    }

    internal class CookieComponent : Component
    {
        public Dictionary<string, string> Cookies = new Dictionary<string, string>();
        public CookieComponent(Request request, string header) : base(request)
        {
            Cookies = header
                .Substring("Cookie: ".Length)
                .Split(';')
                .ToList().Select(t => t.Split('='))
                .GroupBy(q => q[0].Trim(' '), q => q[1])
                .ToDictionary(s => s.Key, s => s.First());
        }

        public string SessionID
        {
            get
            {
                if (Cookies.Keys.Contains("sessionid"))
                    return Cookies["sessionid"];
                return null;
            }
        }
    }

    internal class ResponseComponent : Component
    {
        public byte[] Content;
        internal long ContentLength;

        public virtual string ContentMimeType { get; set; }

        public ResponseComponent(Request request) : base(request)
        {

        }
        public ResponseComponent(Request request, string resposne) : this(request)
        {
            this.Content = Encoding.ASCII.GetBytes(resposne);
        }

    }
    internal class JSONResponse : ResponseComponent
    {
        // "application/json" //JSON text
        // "application/javascript" // JSONP - application/javascript
        public override string ContentMimeType { get { return "application/json"; } }
        public JSONResponse(Request request, string resposne) : base(request, resposne)
        {

        }
    }
    internal class XMLResposne : ResponseComponent
    {
        public XMLResposne(Request request, string resposne) : base(request, resposne)
        {

        }
    }

    internal class SessionComponent : Component
    {
        internal class Session
        {
            public string group;

            public string SessionID { get; internal set; }
        }

        bool CreateSession = false;


        static Dictionary<string, Session> _Sessions;
        static Dictionary<string, Session> Sessions
        {
            get
            {
                if (_Sessions == null)
                    _Sessions = new Dictionary<string, Session>();

                return _Sessions;
            }
        }

        public Session MySession { get; set; }

        public SessionComponent(Request request) : base(request)
        {
            string ckSessionID = null;

            if (request.Cookie != null)
                ckSessionID = request.Cookie.SessionID;


            if (!string.IsNullOrEmpty(ckSessionID))
                if (Sessions.Keys.Contains(ckSessionID))
                    MySession = Sessions[ckSessionID];

            Debug.WriteLineIf(MySession != null, $"\t\t[Sessio] existing session  {MySession?.SessionID}");

            if (MySession == null)
            {


                MySession = new Session()
                {
                    group = "unauthenticated",
                    SessionID = Guid.NewGuid().ToString("n")
                };

                AdditionalHeaders.Add($"Set-Cookie: sessionid={MySession.SessionID}; Path=/");

                Debug.WriteLine("\t\t[Sessio] new session {0}", MySession.SessionID, null);

                Sessions.Add(MySession.SessionID, MySession);
            }
            
        }
    }

    internal class AuthorizationComponent : Component
    {
        public AuthorizationComponent(Request request) : base(request)
        {

        }
        internal virtual bool Authorized { get; set; }
    }
}
