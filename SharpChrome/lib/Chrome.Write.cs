using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using SharpChrome.Extensions;
using SQLite;

namespace SharpChrome
{
    internal partial class Chrome
    {
        /// <summary>
        /// Write a bunch of <see cref="logins"/> to a 'Login Data' database in a given directory.
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <param name="logins"></param>
        /// <param name="newAesStateKey"></param>
        /// <param name="browser"></param>
        /// <param name="progress"></param>
        public static void WriteLocalChromiumLogins(string targetDirectory, IEnumerable<logins> logins,
            byte[] newAesStateKey, Browser browser = Browser.Chrome, IProgress<string> progress = null)
        {
            if (newAesStateKey == null || newAesStateKey.Length == 0) throw new ArgumentNullException(nameof(newAesStateKey));

            if (browser == Browser.Edge) {
                var edgeProcesses = Process.GetProcessesByName("msedge");
                if (edgeProcesses.Any()) {
                    progress?.Report("Edge is running, will attempt forcible close.");
                    edgeProcesses.TryKillProcesses();
                }

                Thread.Sleep(1000);
                
                edgeProcesses = Process.GetProcessesByName("msedge");
                if (edgeProcesses.Any(ep => !ep.HasExited)) {
                    throw new Exception("Unable to kill edge! Try running 'taskkill /im msedge.exe /f' from a command prompt manually.");
                }
            }

            var dbPath = browser.ResolveLoginDataPath(targetDirectory);
            if (!File.Exists(dbPath)) throw new FileNotFoundException("DB 'Login Data' file does not exist!", dbPath);
            progress?.Report($"Writing to: {dbPath}...");

            var uri = new Uri(dbPath);
            // string loginDataFilePathUri = $"{uri.AbsoluteUri}";
            string loginDataFilePathUri = dbPath;
            SQLiteConnection database = null;

            List<logins> loginList = logins.ToList();

            List<logins> loginsWithoutDecryptedPassword = loginList
                .Where(l => string.IsNullOrEmpty(l.DecryptedPasswordValue))
                .ToList();

            List<logins> loginsWithDecryptedPasswords = loginList.Except(loginsWithoutDecryptedPassword).ToList();

            if (loginsWithDecryptedPasswords.Count == 0)
                throw new Exception("Nothing to write! Need a sequence of logins that have decrypted password values.");

            loginsWithDecryptedPasswords = loginsWithDecryptedPasswords.ReEncryptPasswords(newAesStateKey);

            progress?.Report($"Writing {loginsWithDecryptedPasswords.Count} usable logins to {browser.GetBrowserName()}");

            var sqLiteConnectionString = new SQLiteConnectionString(dbPath, SQLiteOpenFlags.ReadWrite, false);

            try {
                database = new SQLiteConnection(loginDataFilePathUri, SQLiteOpenFlags.ReadWrite, false);
            }
            catch (TypeInitializationException tie) {
                var theConnectionString = sqLiteConnectionString.ToString();
                database = new SQLiteConnection(theConnectionString, SQLiteOpenFlags.ReadWrite,
                    false);
            }

            using (database) {
                foreach (var login in loginsWithDecryptedPasswords) {
                    var r = database.InsertOrReplace(login);

                    if (r == 0 || r < 0) throw new Exception("IO error!?");
                }
            }
        }
    }
}