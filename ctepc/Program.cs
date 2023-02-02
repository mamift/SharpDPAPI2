using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using SharpChrome;
using SharpChrome.Extensions;
using Spectre.Console;

namespace ctepc
{
    public class ProgressReporter : IProgress<string>
    {
        public void Report(string value)
        {
            AnsiConsole.WriteLine(value);
        }
    }

    internal static class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("fake") || args.Contains("test")) {
                AnsiConsole.WriteLine("Running test on fake data...");

                InstallFakes();

                return;
            }

            var progressReporter = new ProgressReporter();

            AnsiConsole.WriteLine($"Syncing passwords from chrome to edge...");
            SyncPasswordsFromChromeToEdge(progress: progressReporter);
        }

        private static void InstallFakes()
        {
            var fakes = Fakes.FakeLogins(Browser.Chrome).ToList();

            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");

            var targetBrowser = Browser.Edge;
            var edgeKey = Chrome.GetChromiumStateKey(userDir, targetBrowser);
            
            Chrome.WriteLocalChromiumLogins(userDir, fakes, edgeKey, targetBrowser);
        }

        static void SyncPasswordsFromChromeToEdge(IProgress<string> progress = null)
        {
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            var chromiumLogins = Chrome.ReadLocalChromiumLogins(userProfile, Browser.Chrome, progress);
            var edgeStateKey = Chrome.GetChromiumStateKey(userProfile, Browser.Edge);
            
            Chrome.WriteLocalChromiumLogins(userProfile, chromiumLogins, edgeStateKey, Browser.Edge, progress);
        }
    }
}
