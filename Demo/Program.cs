using SimpleWebServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {

            Trace.Listeners[0].TraceOutputOptions = TraceOptions.None;
            Trace.UseGlobalLock = true;

            var server = new ServerThread();
            server.Start();

            System.Diagnostics.Process.Start("http://localhost:65125/index.html");


            while (true)
            {
                if (server.test != null)
                    server.test.Send("aaaa");
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
