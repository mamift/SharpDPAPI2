using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpChrome;
using SharpChrome.Extensions;

namespace SharpChromeTests
{
    [TestClass]
    public class PasswordWritingTests: BaseTester
    {
        [TestMethod]
        public void ReadFromChromeAndWriteToEdge()
        {
            var logins = Chrome.ReadLocalChromiumLogins(@"C:\temp\chrome", Browser.Chrome);

            var edgeStateKey = Chrome.GetChromiumStateKey(@"C:\temp\edge", Browser.Edge);
            
            Chrome.WriteLocalChromiumLogins(@"C:\temp\edge", logins, Browser.Edge, edgeStateKey);
        }

        [TestMethod]
        public void TestWritingTestPassword()
        {
            var now = DateTime.Now.ToChromeTime();

            var newLogin = new logins() {
                origin_url = "https://mcculloughweb.mcr.au/home/default.aspx",
                password_value = null,
                signon_realm = "https://mcculloughweb.mcr.au/",
                date_created = now,
                blacklisted_by_user = default,
                scheme = default,
                date_last_used = default,
                date_password_modified = now
            };
        }
    }
}