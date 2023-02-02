using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using SharpChrome;
using SharpChrome.Extensions;

namespace ctepc
{
    public class ProgressReporter : IProgress<string>
    {
        public void Report(string value)
        {
            Console.WriteLine(value);
        }
    }

    internal static partial class Program
    {
        static void Main(string[] args)
        {
            var progressReporter = new ProgressReporter();

            #if FAKES
            if (args.Contains("fake") || args.Contains("test")) {
                Console.WriteLine("Running test on fake data...");

                InstallFakes(progressReporter);

                return;
            }
            #endif
            
            Console.WriteLine($"Syncing passwords from chrome to edge...");
            SyncPasswordsFromChromeToEdge(progress: progressReporter);
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
