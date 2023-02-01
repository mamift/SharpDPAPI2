using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Policy;
using System.Text;
using PInvoke;
using SharpChrome.Extensions;
using SQLite;

namespace SharpChrome
{
    internal partial class Chrome
    {
        public const string LoginDataPathFormattedTemplate = "{0}\\AppData\\Local\\{1}\\{2}\\User Data\\Default\\Login Data";
        public const string LocalStatePathFormattedTemplate = "{0}\\AppData\\Local\\{1}\\{2}\\User Data\\Local State";

        public static void SyncChromiumLogins(Dictionary<string, string> masterKeys, string computerName = "",
            string userFolder = "", bool unprotect = false,
            Browser fromBrowser = Browser.Chrome, Browser toBrowser = Browser.Edge, bool quiet = false)
        {
            var userDirectories = GatherUserProfileDirectories(computerName, userFolder, fromBrowser, quiet, masterKeys);

            if (userDirectories.Any(ud => ud.Contains(Environment.GetEnvironmentVariable("USERPROFILE")))) {
                unprotect = true;
            }

            const string loginDataPathFormattedTemplate = "{0}\\AppData\\Local\\{1}\\{2}\\User Data\\Default\\Login Data";
            const string localStatePathFormattedTemplate = "{0}\\AppData\\Local\\{1}\\{2}\\User Data\\Local State";
            
            foreach (string userDirectory in userDirectories) {
                var chromeLoginDataPath = string.Format(loginDataPathFormattedTemplate, userDirectory, "Google", "Chrome");
                var chromeLoginDb = new FileInfo(chromeLoginDataPath).CopyTo(Path.GetTempFileName(), true);

                //var chromeLoginDataPath = $@"C:\temp\chrome\Login Data";
                var chromeAesStateKeyPath = string.Format(localStatePathFormattedTemplate, userDirectory, "Google", "Chrome");
                var chromeAesStateKeyFile = new FileInfo(chromeAesStateKeyPath).CopyTo(Path.GetTempFileName(), true);
                //var chromeAesStateKeyPath = $@"C:\temp\chrome\Local State";

                byte[] chromeAesStateKey = GetStateKey(masterKeys, chromeAesStateKeyFile.FullName, unprotect, quiet);

                var chromeLogins = ParseAndReturnChromeLogins(chromeLoginDb.FullName, chromeAesStateKey);
            }
            
            foreach (string userDirectory in userDirectories) {
                var edgeLoginDataPath = string.Format(loginDataPathFormattedTemplate, userDirectory, "Microsoft", "Edge");
                //var edgeLoginDataPath = $@"C:\temp\edge\Login Data";
                var edgeAesStateKeyPath = string.Format(localStatePathFormattedTemplate, userDirectory, "Microsoft", "Edge");
                //var edgeAesStateKeyPath = $@"C:\temp\edge\Local State";
                
                byte[] edgeAesStateKey = GetStateKey(masterKeys, edgeAesStateKeyPath, unprotect, quiet);
                
                var edgePasswords = ParseAndReturnChromeLogins(edgeLoginDataPath, edgeAesStateKey);
            }
        }

        public static List<logins> ReadLocalChromiumLogins(string directory, Browser browser, bool unprotect = false, 
            bool quiet = false)
        {
            if (directory.Contains(Environment.GetEnvironmentVariable("USERPROFILE"))) {
                unprotect = true;
            }

            var loginDataPath = browser.ResolveLoginDataPath(directory);
            var loginDb = new FileInfo(loginDataPath).CopyTo(Path.GetTempFileName(), true);
            var aesStateKeyPath = browser.ResolveLoginStatePath(directory);
            var aesStateKeyFile = new FileInfo(aesStateKeyPath).CopyTo(Path.GetTempFileName(), true);

            byte[] aesStateKey = GetChromiumStateKey(aesStateKeyPath, browser);

            var logins = ParseAndReturnChromeLogins(loginDb.FullName, aesStateKey);

            return logins;
        }

        public static List<string> GatherUserProfileDirectories(string computerName, string userFolder,
            Browser browser, bool quiet, Dictionary<string, string> masterKeys = null, IProgress<string> progress = null)
        {
            if (masterKeys == null) masterKeys = new Dictionary<string, string>();
            // triage all Chromium 'Login Data' files we can reach
            var userDirectories = new List<string>();

            if (!string.IsNullOrEmpty(computerName)) {
                // if we're triaging a remote computer, check connectivity first
                if (!SharpDPAPI.Helpers.TestRemote(computerName)) {
                    return userDirectories;
                }

                if (!string.IsNullOrEmpty(userFolder)) {
                    // if we have a user folder as the target to triage
                    userDirectories.Add(userFolder);
                }
                else {
                    // Assume C$ (vast majority of cases)
                    string userDirectoryBase = $"\\\\{computerName}\\C$\\Users\\";
                    userDirectories.AddRange(Directory.GetDirectories(userDirectoryBase));
                }
            }
            else if (!string.IsNullOrEmpty(userFolder)) {
                // if we have a user folder as the target to triage
                userDirectories.Add(userFolder);
            }
            else if (SharpDPAPI.Helpers.IsHighIntegrity()) {
                if ($"{System.Security.Principal.WindowsIdentity.GetCurrent().User}" == "S-1-5-18") {
                    // if we're SYSTEM
                    if (masterKeys.Count > 0) {
                        if (!quiet) {
                            progress?.Report(
                                $"\r\n[*] Triaging {SharpDPAPI.Helpers.Capitalize(browser.ToString())} Logins for ALL users\r\n");
                        }

                        userDirectories = SharpDPAPI.Helpers.GetUserFolders();
                    }
                    else {
                        if (!quiet){
                            progress?.Report(string.Format("\r\n[!] Running as SYSTEM but no masterkeys supplied!"));
                        }

                        return userDirectories;
                    }
                }
                else if (masterKeys.Count == 0) {
                    // if we're elevated but not SYSTEM, and no masterkeys are supplied, assume we're triaging just the current user
                    if (!quiet) {
                        progress?.Report(
                            $"\r\n[*] Triaging {SharpDPAPI.Helpers.Capitalize(browser.ToString())} Logins for current user\r\n");
                    }

                    userDirectories.Add(System.Environment.GetEnvironmentVariable("USERPROFILE"));
                }
                else {
                    // otherwise we're elevated and have masterkeys supplied, so assume we're triaging all users
                    if (!quiet) {
                        progress?.Report(
                            $"\r\n[*] Triaging {SharpDPAPI.Helpers.Capitalize(browser.ToString())} Logins for ALL users\r\n");
                    }

                    userDirectories = SharpDPAPI.Helpers.GetUserFolders();
                }
            }
            else {
                // not elevated, no user folder specified, so triage current user
                if (!quiet) {
                    progress?.Report(
                        $"\r\n[*] Triaging {SharpDPAPI.Helpers.Capitalize(browser.ToString())} Logins for current user\r\n");
                }

                userDirectories.Add(System.Environment.GetEnvironmentVariable("USERPROFILE"));
            }

            return userDirectories;
        }
    }

    public class ExtractedPassword
    {
        public string signon_realm { get; set; }
        public string origin_url { get; set; }
        public DateTime? date_created { get; set; }
        public string times_used { get; set; }
        public string username { get; set; }
        public string password { get; set; }

        public logins ToWritableLogin()
        {
            return new logins() {
                signon_realm = this.signon_realm,
                origin_url = this.origin_url,
                date_created = this.date_created.GetValueOrDefault().ToBinary(),
                times_used = int.Parse(this.times_used),
                username_value = this.username,
                password_value = Encoding.Default.GetBytes(this.password)
            };
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global"), SuppressMessage("ReSharper", "InconsistentNaming")]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class logins
    {
        private string _decryptedPasswordValue;

        /// <summary>Required when saving </summary>
        public string origin_url { get; set; }
        public string action_url { get; set; }
        public string username_element { get; set; }
        public string username_value { get; set; }
        public string password_element { get; set; }
        /// <summary>The encrypted password.</summary>
        public byte[] password_value { get; set; }
        public string submit_element { get; set; }
        /// <summary>Required when saving </summary>
        public string signon_realm { get; set; }
        /// <summary>Required when saving </summary>
        public double date_created { get; set; }
        /// <summary>Required when saving </summary>
        public int blacklisted_by_user { get; set; }
        /// <summary>Required when saving </summary>
        public int scheme { get; set; }
        public int password_type { get; set; }
        public int times_used { get; set; }
        public byte[] form_data { get; set; }
        public string display_name { get; set; }
        public string icon_url { get; set; }
        public string federation_url { get; set; }
        public int skip_zero_click { get; set; }
        public int generation_upload_status { get; set; }
        public byte[] possible_username_pairs { get; set; }
        public int id { get; set; }
        /// <summary>Required when saving </summary>
        public double date_last_used { get; set; }
        public byte[] moving_blocked_for { get; set; }
        /// <summary>Required when saving </summary>
        public double date_password_modified { get; set; }

        public string DecryptedPasswordValue => _decryptedPasswordValue;

        public void setDecrypted_password_value(string value) => _decryptedPasswordValue = value;

        private string GetDebuggerDisplay() => ToString();

        public override string ToString()
        {
            var debugStr = $"username = {username_value}, website = {new Uri(origin_url).Host}";
            if (!string.IsNullOrEmpty(DecryptedPasswordValue)) {
                debugStr += $", pass = {DecryptedPasswordValue}";
            }

            return debugStr;
        }

        public BinaryChromePass ToBinaryChromePass()
        {
            return password_value?.ToSegmentedChromePass();
        }
    }

    internal partial class Chrome
    {
        public static List<logins> ParseAndReturnChromeLogins(string loginDataFilePath, byte[] aesStateKey)
        {
            // takes an individual 'Login Data' file path and performs decryption/triage on it
            if (!File.Exists(loginDataFilePath)) {
                return default;
            }

            if (aesStateKey != null) {
                // initialize the BCrypt key using the new DPAPI decryption method
                DPAPIChromeAlgKeyFromRaw(aesStateKey, out var hAlg, out var hKey);
            }

            // convert to a file:/// uri path type so we can do lockless opening
            //  ref - https://github.com/gentilkiwi/mimikatz/pull/199
            var uri = new System.Uri(loginDataFilePath);
            string loginDataFilePathUri = $"{uri.AbsoluteUri}?nolock=1";
            
            SQLiteConnection database = null;

            try {
                database = new SQLiteConnection(loginDataFilePathUri,
                    SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.OpenUri, false);
            }
            catch (Exception e) {
                Console.WriteLine("[X] {0}", e.InnerException.Message);
                return default;
            }
            
            string everyColQuery = "SELECT * FROM logins";
            
            List<logins> allLogins = database.Query<logins>(everyColQuery, false);
            var allLoginsDecryptedPwd = allLogins.DecryptPasswords(aesStateKey);
            
            database.Close();

            return allLoginsDecryptedPwd;
        }

        public static byte[] GetSubArraySansV10(byte[] dwData)
        {
            byte[] subArrayNoV10 = new byte[dwData.Length - DPAPI_CHROME_UNKV10.Length];
            Array.Copy(dwData, 3, subArrayNoV10, 0, dwData.Length - DPAPI_CHROME_UNKV10.Length);

            return subArrayNoV10;
        }

        /// <summary>
        /// Encrypts data for saving inside Chromium's 'Login Data' database file.
        /// </summary>
        /// <param name="dataToEncrypt">Data to encrypt</param>
        /// <param name="aesEncryptionKey"></param>
        /// <param name="nonce"></param>
        /// <param name="gcmTag"></param>
        /// <returns></returns>
        public static byte[] EncryptAESChromeBlob(byte[] dataToEncrypt, byte[] aesEncryptionKey, byte[] nonce, byte[] gcmTag)
        {
            var encrypted = AESGCM.GcmEncrypt(dataToEncrypt, aesEncryptionKey, nonce, gcmTag);

            var v10HeaderAndIv = DPAPI_CHROME_UNKV10.ArrayConcat(nonce);
            var v10HeaderAndIvAndEncryptedData = v10HeaderAndIv.ArrayConcat(encrypted);
            var v10HeaderAndIvAndEncryptedDataAndTag = v10HeaderAndIvAndEncryptedData.ArrayConcat(gcmTag);
                
            return v10HeaderAndIvAndEncryptedDataAndTag;
        }

        /// <summary>
        /// Encrypts data for saving inside Chromium's 'Login Data' database file.
        /// </summary>
        /// <param name="dataToEncrypt">Data to encrypt</param>
        /// <param name="aesEncryptionKey"></param>
        /// <param name="chPass"></param>
        /// <returns></returns>
        public static byte[] EncryptAESChromeBlob(byte[] dataToEncrypt, byte[] aesEncryptionKey, BinaryChromePass chPass)
        {
            var encrypted = AESGCM.GcmEncrypt(dataToEncrypt, aesEncryptionKey, chPass.InitVector, chPass.Tag);

            var v10HeaderAndIv = DPAPI_CHROME_UNKV10.ArrayConcat(chPass.InitVector);
            var v10HeaderAndIvAndEncryptedData = v10HeaderAndIv.ArrayConcat(encrypted);
            var v10HeaderAndIvAndEncryptedDataAndTag = v10HeaderAndIvAndEncryptedData.ArrayConcat(chPass.Tag);
                
            return v10HeaderAndIvAndEncryptedDataAndTag;
        }
    }
}