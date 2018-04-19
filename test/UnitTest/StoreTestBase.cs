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
                ClientName = "test",
                AccessTokenType = AccessTokenType.Reference
            };
        }
        public Secret CreateSecret()
        {
            return new Secret()
            {
                Description = Guid.NewGuid().ToString("n"),
                Expiration = new DateTime(2200,1,1),
                Type = "type",
                Value = Guid.NewGuid().ToString("n")
            };
        }
        public PersistedGrant CreateGrant(string subjectId = null, string type = null)
        {
            return new PersistedGrant()
            {
                ClientId = "client",
                Expiration = new DateTime(2200,1,1),
                SubjectId = subjectId ?? Guid.NewGuid().ToString("n"),
                CreationTime = new DateTime(2190,1,1),
                Data = Guid.NewGuid().ToString("n"),
                Key = Guid.NewGuid().ToString("n"),
                Type = type ?? Guid.NewGuid().ToString("n")
            };
        }

        public ApiResource CreateApiResource()
        {
            return new ApiResource()
            {
                Name = Guid.NewGuid().ToString("n"),
                Description = "description"
            };
        }
        public IdentityResource CreateIdentityResource()
        {
            return new IdentityResource()
            {
                Name = Guid.NewGuid().ToString("n"),
                Description = "description"
            };
        }
        public Scope CreateScope()
        {
            return new Scope()
            {
                Name = Guid.NewGuid().ToString("n"),
                Description = "description"
            };
        }
    }
}
