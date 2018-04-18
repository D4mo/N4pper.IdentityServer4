using System;
using System.Collections.Generic;
using System.Text;

namespace N4pper.IdentityServer4.Model
{
    public interface IGraphEntity
    {
        long? EntityId { get; set; }
    }
}
