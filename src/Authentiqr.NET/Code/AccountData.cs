using Authentiqr.Core;
using Authentiqr.Core.Encode;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace Authentiqr.NET.Code
{
    public class AccountData : Dictionary<string, SecureString>
    {
        public string Data
        {
            get
            {
                return string.Join("*", this.Select(account =>
                    account.Value.Use(password =>
                        string.Format("{0}@{1}",
                                      Base64.Encode(Encoding.UTF8.GetBytes(account.Key)),
                                      Base64.Encode(Encoding.UTF8.GetBytes(password))))));
            }
        }

        public static AccountData Parse(string data)
        {
            if (string.IsNullOrEmpty(data))
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
