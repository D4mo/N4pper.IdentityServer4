using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    public abstract class StoreTestBase
    {
        public string CreateClientId(string suffix, bool suffixAsClientId = false)
        {
            return suffixAsClientId ? suffix : Guid.NewGuid().ToString("N") + suffix;
        }
        public Client CreateClient()
        {
            return new Client()
            {
                ClientId = CreateClientId(""),
                RedirectUris = new List<string>() { "aaa", "bbb" },
                ClientName = "test"
            };
        }
        public Secret CreateSecret()
        {
            return new Secret()
            {
                Description = Guid.NewGuid().ToString("n"),
                Expiration = new DateTime(1970,1,1),
                Type = "type",
                Value = Guid.NewGuid().ToString("n")
            };
        }
    }
}
