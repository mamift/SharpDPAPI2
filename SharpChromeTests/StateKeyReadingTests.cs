using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpChrome;
using SharpChrome.Extensions;
using SharpDPAPI;

namespace SharpChromeTests
{
    [TestClass]
    public class StateKeyReadingTests: BaseTester
    {
        [TestMethod]
        public void TestReadingStateKeyFromMicrosoftEdge()
        {
            var dir = Environment.GetEnvironmentVariable("USERPROFILE");
            var browser = Browser.Edge;
            var key = Chrome.GetChromiumStateKey(dir, browser);

            Assert.IsNotNull(key);
            Assert.IsTrue(key.Length == 32);

            TestContext.WriteLine($"Browser is: {browser.GetBrowserName()}");
            TestContext.WriteLine("Key is: " + Helpers.ByteArrayToString(key));
        }

        [TestMethod]
        public void TestReadingStateKeyFromGoogleChrome()
        {
            var dir = Environment.GetEnvironmentVariable("USERPROFILE");
            var browser = Browser.Chrome;
            var key = Chrome.GetChromiumStateKey(dir, browser);

            Assert.IsNotNull(key);
            Assert.IsTrue(key.Length == 32);

            TestContext.WriteLine($"Browser is: {browser.GetBrowserName()}");
            TestContext.WriteLine("Key is: " + Helpers.ByteArrayToString(key));
        }

        [TestMethod]
        public void TestReadingStateKeyFromTestDir()
        {
            var testDir = @"C:\temp\chrome";

            var key = Chrome.GetChromiumStateKey(testDir);

            Assert.IsNotNull(key);
            Assert.IsTrue(key.Length == 32);

            TestContext.WriteLine("Key is: " + Helpers.ByteArrayToString(key));
        }
    }
}