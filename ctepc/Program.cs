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
            Console.WriteLine($"NOTE: Running this multiple times may override data. Only supports copying from Chrome to Edge, and not vice versa.");

            #if FAKES
            if (args.Contains("fake") || args.Contains("test")) {
                Console.WriteLine("Running test on fake data...");

                InstallFakes(progressReporter);

                return;
            }
            #endif
            
            Console.WriteLine($"Copying passwords from chrome to edge...");
            CopyPasswordsFromChromeToEdge(progress: progressReporter);

            Console.WriteLine($"Copying bookmarks from chrome to edge...");
            CopyBookmarksFromChromeToEdge(progressReporter);
        }

        static void CopyPasswordsFromChromeToEdge(IProgress<string> progress = null)
        {
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            var chromiumLogins = Chrome.ReadLocalChromiumLogins(userProfile, Browser.Chrome, progress);
            var edgeStateKey = Chrome.GetChromiumStateKey(userProfile, Browser.Edge);
            
            Chrome.WriteLocalChromiumLogins(userProfile, chromiumLogins, edgeStateKey, Browser.Edge, progress);
        }

        static void CopyBookmarksFromChromeToEdge(IProgress<string> progress = null)
        {
            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");
            var chromiumBookmarks = SharpChrome.Chrome.GetChromiumBookmarks(userDir, Browser.Chrome, progress);
            
            Chrome.SetChromiumBookmarks(userDir, chromiumBookmarks, Browser.Edge, progress);
        }
    }
}
