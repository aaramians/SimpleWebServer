using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleWebServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebServer.Tests
{
    [TestClass()]
    public class ServerTests
    {
        [TestMethod()]
        public void ListenTest()
        {
            var t = new ServerThread();
            t.Start();
            Assert.Fail();
        }
    }
}