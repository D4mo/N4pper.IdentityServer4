using IdentityServer4.Configuration;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver.V1;
using OMnG;
using System;
using System.Collections.Generic;
using System.Text;

namespace N4pper.IdentityServer4
{
    public static class IServiceCollectionExtensions
    {
        #region nested types

        private class InternalDriverProvider : IdentityServerDriverProvider
        {
            public InternalDriverProvider(string uri, IAuthToken authToken, Config config, N4pperManager manager)
                : base(manager)
            {
                if (string.IsNullOrEmpty(uri))
                    throw new ArgumentNullException(nameof(uri));

                _uri = uri;
                _authToken = authToken ?? AuthTokens.None;
                _config = config ?? new Config();
            }

            private string _uri;
            private IAuthToken _authToken;
            private Config _config;

            public override string Uri => _uri;

            public override IAuthToken AuthToken => _authToken;

            public override Config Config => _config;
        }

        #endregion

        public static IServiceCollection AddIdentityServer4Neo4jStores(this IServiceCollection ext, Options options, IdentityServerOptions idsOptions = null)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            options = options ?? throw new ArgumentNullException(nameof(options));
            idsOptions = idsOptions ?? new IdentityServerOptions();

            ext.AddN4pper();
            
            ext.AddSingleton<IdentityServerDriverProvider>(provider => new InternalDriverProvider(options.Uri, options.Token, options.Configuration, provider.GetRequiredService<N4pperManager>()));

            IIdentityServerBuilder builder = ext.AddIdentityServer(opt=>opt.CopyProperties(idsOptions));

            builder.Services.AddTransient<IClientStore, Neo4jClientStore>();
            builder.Services.AddTransient<IPersistedGrantStore, Neo4jPersistedGrantStore>();
            builder.Services.AddTransient<IResourceStore, Neo4jResourceStore>();

            return ext;
        }
    }
}
