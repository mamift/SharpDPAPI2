﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Community.CsharpSqlite;
using SharpDPAPI;
using static Community.CsharpSqlite.Sqlite3;
using DateTime = System.DateTime;

namespace SharpChrome.Extensions
{
    public static class GeneralExtensions
    {
        /// <summary>
        /// Reads the current <see cref="Stream"/> as a <see cref="string"/>, optionally using a given <paramref name="encoding"/>.
        /// <para>Will also reset the Stream's internal position to 0 so it can be re-read again.</para>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static async Task<string> ReadAsStringAsync(this Stream stream, Encoding encoding = null)
        {
            stream.Position = 0;
            var streamReader = encoding == null
                ? new StreamReader(stream, detectEncodingFromByteOrderMarks: true)
                : new StreamReader(stream, encoding);

            var @string = await streamReader.ReadToEndAsync();
            stream.Position = 0; // reset position
            return @string;
        }

        /// <summary>
        /// Reads the current <see cref="Stream"/> as a <see cref="string"/>, optionally using a given <paramref name="encoding"/>.
        /// <para>Will also reset the Stream's internal position to 0 so it can be re-read again.</para>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ReadAsString(this Stream stream, Encoding encoding = null)
        {
            stream.Position = 0;
            var streamReader = encoding == null
                ? new StreamReader(stream, detectEncodingFromByteOrderMarks: true)
                : new StreamReader(stream, encoding);

            var @string = streamReader.ReadToEnd();
            stream.Position = 0; // reset position
            return @string;
        }

        public static List<logins> DecryptPasswords(this IEnumerable<logins> logins, byte[] aesStateKey)
        {
            if (aesStateKey == null || aesStateKey.Length == default) throw new ArgumentNullException(nameof(aesStateKey));

            byte[] iv = new byte[12];
            BCrypt.SafeAlgorithmHandle hAlg = null;
            BCrypt.SafeKeyHandle hKey = null;

            var keyDecryptResult = Chrome.DPAPIChromeAlgKeyFromRaw(aesStateKey, out hAlg, out hKey);
            if (false == keyDecryptResult) throw new Exception("Unable to determine algorithm key!");
            var loginsList = logins.ToList();

            foreach (var login in loginsList) {
                byte[] passwordBytes = login.password_value;
                byte[] decBytes = null;
                string password = null;

                if (HasV10Header(passwordBytes)) {
                    // using the new DPAPI decryption method
                    decBytes = Chrome.DecryptAESChromeBlob(passwordBytes, hAlg, hKey, out iv);

                    if (decBytes == null) {
                        throw new Exception($"Unable to decrypt {nameof(login.password_value)} ({login.username_value} :: {login.origin_url})");
                    }
                }
                else {
                    // using the old method
                    Dictionary<string, string> masterKeys = new Dictionary<string, string>();
                    decBytes = Dpapi.DescribeDPAPIBlob(blobBytes: passwordBytes, MasterKeys: masterKeys, blobType: "chrome");
                }

                password = Encoding.ASCII.GetString(decBytes);

                byte[] reEncrypted = Chrome.EncryptAESChromeBlob(decBytes, hAlg, hKey, iv);

                var nonce = BouncyCastleExtensions.GetNonce(passwordBytes);
                var encryptedWithBc = BouncyCastleExtensions.EncryptWithGcm(decBytes, aesStateKey, nonce);
                var reDecryptedWithBc = BouncyCastleExtensions.DecryptWithGcm(encryptedWithBc, aesStateKey, nonce);

                var en_good = Helpers.ByteArrayToString(passwordBytes);
                var en_good2 = Helpers.ByteArrayToString(reEncrypted);

                var passwordBytes2 = Chrome.DecryptAESChromeBlob(reEncrypted, hAlg, hKey, out var _);
                var password2_utf = Encoding.UTF8.GetString(passwordBytes2);
                var password2_ascii = Encoding.ASCII.GetString(passwordBytes2);

                var en_reCreation = Helpers.ByteArrayToString(encryptedWithBc);

                var de_good = Helpers.ByteArrayToString(decBytes);
                var de_reCreation = Helpers.ByteArrayToString(reDecryptedWithBc);
                
                //var reDecryptedWithBc2 = BouncyCastleExtensions.DecryptWithGcm(passwordBytes, aesStateKey, nonce);

                login.setDecrypted_password_value(password);
            }


            return loginsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasV10Header(this byte[] data) => Chrome.HasV10Header(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime FromChromeTimeToDateTime(this double bigIntegerValue) => Helpers.ConvertToDateTime(bigIntegerValue);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FromDateTimeToChromeTime(this DateTime dateTimeValue) => Helpers.ConvertToChromeTime(dateTimeValue);

        /// <summary>
        /// Concatenates the current array with the <paramref name="otherArray"/>, returning a new array.
        /// <para>The current array begins the new array.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="otherArray"></param>
        /// <returns></returns>
        public static T[] ArrayConcat<T>(this T[] array, T[] otherArray)
        {
            T[] concatenated = new T[array.Length + otherArray.Length];
            array.CopyTo(concatenated, 0);
            otherArray.CopyTo(concatenated, array.Length);

            return concatenated;
        }

        /// <summary>
        /// Converts a <see cref="byte"/> pointer to a regular <see cref="byte"/> <see cref="Array"/>.
        /// </summary>
        /// <param name="bytePointer"></param>
        /// <param name="arrayLength"></param>
        /// <returns></returns>
        public static unsafe byte[] ToByteArray(byte* bytePointer, int arrayLength)
        {
            if (arrayLength <= 0) throw new ArgumentOutOfRangeException(nameof(arrayLength));

            var byteArray = new byte[arrayLength];
            Marshal.Copy((IntPtr)bytePointer, byteArray, 0, arrayLength);

            return byteArray;
        }
    }
}