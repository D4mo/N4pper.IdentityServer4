using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N4pper.IdentityServer4.Services;
using N4pper.IdentityServer4.Stores;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace N4pper.IdentityServer4
{
    public static class IdentityServerN4pperBuilderExtensions
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
        
        private class TokenCleanupHost : IHostedService
        {
            private readonly TokenCleanup _tokenCleanup;
            private readonly Options _options;

            public TokenCleanupHost(TokenCleanup tokenCleanup, Options options)
            {
                _tokenCleanup = tokenCleanup;
                _options = options;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                if (_options.EnableTokenCleanup)
                {
                    _tokenCleanup.Start(cancellationToken);
                }
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                if (_options.EnableTokenCleanup)
                {
                    _tokenCleanup.Stop();
                }
                return Task.CompletedTask;
            }
        }

        #endregion

        public static IIdentityServerBuilder AddNeo4jConfigurationAndOperationalStore(
            this IIdentityServerBuilder builder,
            Options options)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));

            //configuration
            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<IHostedService, TokenCleanupHost>();

            builder.Services.AddSingleton<IdentityServerDriverProvider>(provider => new InternalDriverProvider(options.Uri, options.Token, options.Configuration, provider.GetRequiredService<N4pperManager>()));

            builder.AddClientStore<Neo4jClientStore>();
            builder.AddResourceStore<Neo4jResourceStore>();
            builder.AddCorsPolicyService<Neo4jCorsPolicyService>();

            //operational
            builder.Services.AddSingleton<TokenCleanup>();
            builder.Services.AddSingleton<IHostedService, TokenCleanupHost>();
            
            builder.Services.AddTransient<IPersistedGrantStore, Neo4jPersistedGrantStore>();

            return builder;
        }

        public static IIdentityServerBuilder AddConfigurationStoreCache(
            this IIdentityServerBuilder builder)
        {
            builder.AddInMemoryCaching();

            // add the caching decorators
            builder.AddClientStoreCache<Neo4jClientStore>();
            builder.AddResourceStoreCache<Neo4jResourceStore>();
            builder.AddCorsPolicyCache<Neo4jCorsPolicyService>();

            return builder;
        }
    }
}
