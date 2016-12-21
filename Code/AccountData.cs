using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Authentiqr.NET.Code
{
    public class AccountData : Dictionary<string, SecureString>
    {
        public string Data
        {
            get
            {
                // Apply a random salt to the data prior to encryption
                var rng = new RNGCryptoServiceProvider();
                var bytes = new byte[1];
                rng.GetBytes(bytes);
                bytes = new byte[32 + bytes[0]];
                rng.GetBytes(bytes);

                var data = String.Join("*", this.Select(account => account.Value.Use(password => String.Format("{0}@{1}",
                                                        Base64.Encode(Encoding.UTF8.GetBytes(account.Key)),
                                                        Base64.Encode(Encoding.UTF8.GetBytes(password))))));

                return String.Format("{0}#{1}", Base64.Encode(bytes), data);
            }
        }

        public static AccountData Parse(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return new AccountData();
            }
            else
            {
                var output = new AccountData();

                var salt = data.IndexOf('#');

                if (salt >= 0)
                {
                    data = data.Substring(salt + 1);
                }

                string[] accountData = data.Split('*');

                foreach (var account in accountData)
                {
                    string[] kvp = account.Split('@');
                    kvp[0] = Encoding.UTF8.GetString(Base64.Decode(kvp[0]));
                    kvp[1] = Encoding.UTF8.GetString(Base64.Decode(kvp[1]));
                    output[kvp[0]] = new SecureString().AppendChars(kvp[1]);
                }

                return output;
            }
        }
    }
}
