using SimpleWebServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debug.AutoFlush = true;
            System.Diagnostics.Trace.AutoFlush = true;
            System.Diagnostics.Trace.UseGlobalLock = true;
            System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());

            var server = new Server();
            server.Listen();
        }
    }
}
