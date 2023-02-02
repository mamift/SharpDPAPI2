using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bogus;
using SharpChrome;
using SharpChrome.Extensions;

namespace ctepc
{
    public static class Fakes
    {
        public static IEnumerable<logins> FakeLogins(Browser sourceBrowser)
        {
            var faker = new Faker("en");
            var fakeFloorDate = new DateTime(1972, 1, 1).ToChromeTime();
            var fakeCeilingDate = DateTime.Now.ToChromeTime();
            var fakeLoginsLimit = faker.Random.UShort(3, 65);

            byte[] aesEncryptKey, iv, tag;

            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");
            aesEncryptKey = Chrome.GetChromiumStateKey(userDir, sourceBrowser);

            var ivAndTagTuple = Chrome.GetIvAndTagFromFirstPassword(userDir, sourceBrowser, aesEncryptKey);
            iv = ivAndTagTuple.Item1;
            tag = ivAndTagTuple.Item2;

            foreach (var iteration in Enumerable.Range(0, fakeLoginsLimit)) {
                var originUrl = faker.Internet.Url();
                var randomPassVal = faker.Random.String2(32);
                //var encryptedPassBytes =
                //    Chrome.EncryptAESChromeBlob(Encoding.UTF8.GetBytes(randomPassVal), aesEncryptKey, null, null);

                var login = new logins() {
                    origin_url = originUrl,
                    action_url = originUrl,
                    signon_realm = new Uri(originUrl).GetLeftPart(UriPartial.Authority),
                    date_created = faker.Random.Double(fakeFloorDate, fakeCeilingDate),
                    blacklisted_by_user = Convert.ToInt16(faker.Random.Bool()),
                    //scheme = faker.Random.UShort(0, 3),
                    scheme = default,
                    date_last_used = 0,
                    date_password_modified = faker.Random.Double(fakeFloorDate, fakeCeilingDate),
                    //password_value = encryptedPassBytes,
                    username_value = faker.Person.UserName,
                };

                login.setDecrypted_password_value(randomPassVal);

                yield return login;
            }
        }
    }
}