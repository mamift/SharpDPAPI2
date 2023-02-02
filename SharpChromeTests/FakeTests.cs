using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpChrome;

namespace SharpChromeTests
{
    [TestClass]
    public class FakeTests: BaseTester
    {
        [TestMethod]
        public void TestFakes()
        {
            var fakes = Fakes.FakeLogins(Browser.Chrome).ToList();

            Assert.IsNotNull(fakes);
            Assert.IsTrue(fakes.Count > 0);

            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");

            var targetBrowser = Browser.Edge;
            var edgeKey = Chrome.GetChromiumStateKey(userDir, targetBrowser);
            
            Chrome.WriteLocalChromiumLogins(userDir, fakes, edgeKey, targetBrowser);
        }
    }
}