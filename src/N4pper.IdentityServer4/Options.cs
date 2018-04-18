using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace N4pper.IdentityServer4
{
    public class Options
    {
        public string Uri { get; set; }
        public IAuthToken Token { get; set; } = AuthTokens.None;
        public Config Configuration { get; set; } = new Config();
    }
}
