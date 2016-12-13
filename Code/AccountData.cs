using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Authentiqr.NET.Code
{
    public class AccountData : Dictionary<string, string>
    {
        public string Data
        {
            get
            {
                return String.Join("*", this.Select(account => String.Format("{0}@{1}",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(account.Key)),
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(account.Value)))));
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

                string[] accountData = data.Split('*');

                foreach (var account in accountData)
                {
                    string[] kvp = account.Split('@');
                    kvp[0] = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(kvp[0]));
                    kvp[1] = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(kvp[1]));
                    output[kvp[0]] = kvp[1];
                }

                return output;
            }
        }
    }
}
