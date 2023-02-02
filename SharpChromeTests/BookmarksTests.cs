using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpChrome;

namespace SharpChromeTests
{
    [TestClass]
    public class BookmarksTests: BaseTester
    {
        [TestMethod]
        public void TestSettingBookmarks(IProgress<string> progress, IProgress<string> progress1)
        {
            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");
            var chromiumBookmarks = SharpChrome.Chrome.GetChromiumBookmarks(userDir, Browser.Chrome, progress);

            Assert.IsNotNull(chromiumBookmarks);

            Chrome.SetChromiumBookmarks(userDir, chromiumBookmarks, Browser.Edge, progress1);
        }

        [TestMethod]
        public void TestGettingBookmarks()
        {
            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");
            var bookmarks = SharpChrome.Chrome.GetChromiumBookmarks(userDir);

            Assert.IsNotNull(bookmarks);


        }
    }
}