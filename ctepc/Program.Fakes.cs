#if FAKES
using System;
using System.Linq;
using SharpChrome;

namespace ctepc
{
    internal static partial class Program
    {
        private static void InstallFakes(IProgress<string> progress = null)
        {
            var fakes = Fakes.FakeLogins(Browser.Chrome).ToList();

            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");

            var targetBrowser = Browser.Edge;
            var edgeKey = Chrome.GetChromiumStateKey(userDir, targetBrowser);

            Chrome.WriteLocalChromiumLogins(userDir, fakes, edgeKey, targetBrowser, progress);
        }
    }
}
#endif