using System;
using System.Collections.Generic;
using System.Text;

namespace N4pper.IdentityServer4.Model
{
    public class Neo4jProperty : IGraphEntity
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public long? EntityId { get; set; }
    }
}
