using System;
using System.Collections.Generic;
using System.Text;

namespace N4pper.IdentityServer4
{
    public abstract class IdentityServerDriverProvider : DriverProvider
    {
        public IdentityServerDriverProvider(N4pperManager manager) : base(manager)
        {
        }
    }
}
