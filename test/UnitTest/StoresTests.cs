using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N4pper;
using N4pper.IdentityServer4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    [TestCaseOrderer(AsIKnow.XUnitExtensions.Constants.PriorityOrdererTypeName, AsIKnow.XUnitExtensions.Constants.PriorityOrdererTypeAssemblyName)]
    [Collection(nameof(Neo4jCollection))]
    public class StoresTests : StoreTestBase
    {
        protected Neo4jFixture Fixture { get; set; }

        protected IdentityServerDriverProvider Provider { get; set; }

        public StoresTests(Neo4jFixture fixture)
        {
            Fixture = fixture;

            Provider = Fixture.GetService<IdentityServerDriverProvider>();
        }

        [Fact]
        public void CanCreateAndUpdateAndDeleteClient()
        {
            IClientStore store = Fixture.GetService<IClientStore>();

            Client client = CreateClient();

            Client result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.Null(result);

            Provider.AddClientAsync(client).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new string[] { "aaa", "bbb" }, result.RedirectUris);
            Assert.Equal("test", result.ClientName);
            Assert.Equal(AccessTokenType.Reference, result.AccessTokenType);

            result.ClientName = "test2";
            result.RedirectUris.Add("ccc");
            result.RedirectUris.Remove("bbb");

            Provider.UpdateClientAsync(result).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new string[] { "aaa", "ccc" }, result.RedirectUris);
            Assert.Equal("test2", result.ClientName);

            List<Client> results = Provider.GetAllClientsAsync().Result.ToList();
            Assert.Equal(1, results.Count);
            Assert.Equal(result.ClientId, results.First().ClientId);

            Provider.RemoveClientAsync(result).Wait();
            result = store.FindClientByIdAsync(client.ClientId).Result;

            Assert.Null(result);
        }

        [Fact]
        public void CanManageClientProperties()
        {
            IClientStore store = Fixture.GetService<IClientStore>();

            Client client = CreateClient();

            Client result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.Null(result);

            Provider.AddClientAsync(client).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new Dictionary<string, string>(), result.Properties);

            Provider.SetClientPropsAsync(result, new Dictionary<string, string>() { { "prop1", "aaa" }, { "prop2", "bbb" } }).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new Dictionary<string, string>() { { "prop1", "aaa" }, { "prop2", "bbb" } }, result.Properties);

            Provider.ReplaceClientPropAsync(result, "prop1", "xxx").Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new Dictionary<string, string>() { { "prop1", "xxx" }, { "prop2", "bbb" } }, result.Properties);

            Provider.RemoveClientPropAsync(result, "prop1").Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new Dictionary<string, string>() { { "prop2", "bbb" } }, result.Properties);

            Provider.ClearAllClientPropsAsync(result).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new Dictionary<string, string>(), result.Properties);
            
            Assert.Throws<AggregateException>(()=>Provider.SetClientPropsAsync(result, null).Wait());
        }

        [Fact]
        public void CanManageClientSecrets()
        {
            IClientStore store = Fixture.GetService<IClientStore>();

            Client client = CreateClient();

            Client result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.Null(result);

            Provider.AddClientAsync(client).Wait();

            List<Secret> args = new List<Secret>() { CreateSecret(), CreateSecret() };

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Secret>(), result.ClientSecrets);

            Provider.SetClientSecretsAsync(result, args).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(args, result.ClientSecrets);
            Assert.NotEqual("test", result.ClientSecrets.First().Type);

            args[0].Type = "test";

            Provider.ReplaceClientSecretAsync(result, args[0]).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(args, result.ClientSecrets);
            Assert.Equal("test", result.ClientSecrets.First().Type);

            Provider.RemoveClientSecretAsync(result, args[0]).Wait();
            args.RemoveAt(0);

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(args, result.ClientSecrets);

            Provider.ClearAllClientSecretsAsync(result).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Secret>(), result.ClientSecrets);

            Assert.Throws<AggregateException>(() => Provider.SetClientSecretsAsync(result, null).Wait());
        }


        [Fact]
        public void CanManageClientClaims()
        {
            IClientStore store = Fixture.GetService<IClientStore>();

            Client client = CreateClient();

            Client result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.Null(result);

            Provider.AddClientAsync(client).Wait();

            List<Claim> args = new List<Claim>() { new Claim("claim1","val1"), new Claim("claim2", "val2") };

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Claim>(), result.Claims);

            Provider.SetClientClaimsAsync(result, args).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(args.Select(p=>new Tuple<string,string>(p.Type, p.Value)), result.Claims.Select(p => new Tuple<string, string>(p.Type, p.Value)));
            Assert.NotEqual("test", result.Claims.First().Value);

            args[0] = new Claim("claim1", "test");

            Provider.ReplaceClientClaimAsync(result, args[0].Type, args[0].Value).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(args.Select(p => new Tuple<string, string>(p.Type, p.Value)), result.Claims.Select(p => new Tuple<string, string>(p.Type, p.Value)));
            Assert.Equal("test", result.Claims.First().Value);

            Provider.RemoveClientClaimAsync(result, args[0].Type).Wait();
            args.RemoveAt(0);

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(args.Select(p => new Tuple<string, string>(p.Type, p.Value)), result.Claims.Select(p => new Tuple<string, string>(p.Type, p.Value)));

            Provider.ClearAllClientClaimsAsync(result).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Claim>(), result.Claims);

            Assert.Throws<AggregateException>(() => Provider.SetClientClaimsAsync(result, null).Wait());
        }


        [Fact]
        public void FetchCompleteClient()
        {
            IClientStore store = Fixture.GetService<IClientStore>();

            Client client = CreateClient();

            Client result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.Null(result);

            Provider.AddClientAsync(client).Wait();

            Dictionary<string, string> argProps = new Dictionary<string, string>() { { "prop1", "aaa" }, { "prop2", "bbb" } };
            List<Secret> argsSecret = new List<Secret>() { CreateSecret(), CreateSecret() };
            List<Claim> argsClaim = new List<Claim>() { new Claim("claim1", "val1"), new Claim("claim2", "val2") };

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Claim>(), result.Claims);

            Provider.SetClientPropsAsync(result, argProps).Wait();
            Provider.SetClientSecretsAsync(result, argsSecret).Wait();
            Provider.SetClientClaimsAsync(result, argsClaim).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(argsClaim.Select(p => new Tuple<string, string>(p.Type, p.Value)), result.Claims.Select(p => new Tuple<string, string>(p.Type, p.Value)));
            Assert.Equal(argsSecret, result.ClientSecrets);
            Assert.Equal(argProps, result.Properties);
        }


        [Fact]
        public void CanCreateAndUpdateAndDeletePersistedGrant()
        {
            IPersistedGrantStore store = Fixture.GetService<IPersistedGrantStore>();

            PersistedGrant grant = CreateGrant();

            PersistedGrant result = store.GetAsync(grant.Key).Result;
            Assert.Null(result);

            Provider.AddPersistedGrantAsync(grant).Wait();
            
            result = store.GetAsync(grant.Key).Result;
            Assert.NotNull(result);
            Assert.NotEqual("test", result.Data);

            result.Data = "test";

            Provider.UpdatePersistedGrantAsync(result).Wait();

            result = store.GetAsync(grant.Key).Result;
            Assert.NotNull(result);
            Assert.Equal("test", result.Data);

            Provider.RemovePersistedGrantAsync(result).Wait();

            result = store.GetAsync(grant.Key).Result;
            Assert.Null(result);
        }

        [Fact]
        public void PersistedGrantStore()
        {
            IPersistedGrantStore store = Fixture.GetService<IPersistedGrantStore>();

            PersistedGrant grant1 = CreateGrant("aaa","t1");
            PersistedGrant grant2 = CreateGrant("aaa");
            PersistedGrant grant3 = CreateGrant("aaa");
            PersistedGrant grant4 = CreateGrant();
            PersistedGrant grant5 = CreateGrant();

            store.StoreAsync(grant1).Wait();
            store.StoreAsync(grant2).Wait();
            store.StoreAsync(grant3).Wait();
            store.StoreAsync(grant4).Wait();
            store.StoreAsync(grant5).Wait();

            List<PersistedGrant> results = store.GetAllAsync("aaa").Result?.ToList();
            Assert.Equal(3, results.Count);

            store.RemoveAllAsync("aaa", grant1.ClientId, "t1").Wait();

            results = store.GetAllAsync("aaa").Result?.ToList();
            Assert.Equal(2, results.Count);

            store.RemoveAllAsync("aaa", grant1.ClientId).Wait();

            results = store.GetAllAsync("aaa").Result?.ToList();
            Assert.Empty(results);

            PersistedGrant result = store.GetAsync(grant5.Key).Result;
            Assert.NotNull(result);

            store.RemoveAsync(grant5.Key);
            result = store.GetAsync(grant5.Key).Result;
            Assert.Null(result);
        }

        [Fact]
        public void CanCreateAndUpdateAndDeleteResources()
        {
            IResourceStore store = Fixture.GetService<IResourceStore>();

            ApiResource apiResource = CreateApiResource();

            Resource result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.Null(result);

            Provider.AddResourceAsync(apiResource).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.NotEqual("test", result.Description);
            Assert.True(result is ApiResource);

            result.Description = "test";
            Provider.UpdateResourceAsync(result).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal("test", result.Description);
            Assert.True(result is ApiResource);
            
            Provider.RemoveResourceAsync(apiResource).Wait();
            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.Null(result);

            IdentityResource idResource = CreateIdentityResource();

            result = store.FindIdentityResourcesByScopeAsync(new string[] { idResource.Name }).Result.FirstOrDefault();
            Assert.Null(result);

            Provider.AddResourceAsync(idResource).Wait();

            result = store.FindIdentityResourcesByScopeAsync(new string[] { idResource.Name }).Result.FirstOrDefault();
            Assert.NotNull(result);
            Assert.NotEqual("test", result.Description);
            Assert.True(result is IdentityResource);

            result.Description = "test";
            Provider.UpdateResourceAsync(result).Wait();

            result = store.FindIdentityResourcesByScopeAsync(new string[] { idResource.Name }).Result.FirstOrDefault();
            Assert.NotNull(result);
            Assert.Equal("test", result.Description);
            Assert.True(result is IdentityResource);

            Provider.RemoveResourceAsync(idResource).Wait();
            result = store.FindIdentityResourcesByScopeAsync(new string[] { idResource.Name }).Result.FirstOrDefault();
            Assert.Null(result);
        }
        
        [Fact]
        public void CanManageApiResourceSecrets()
        {
            IResourceStore store = Fixture.GetService<IResourceStore>();

            ApiResource apiResource = CreateApiResource();

            ApiResource result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.Null(result);

            Provider.AddResourceAsync(apiResource).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Secret>(), result.ApiSecrets);

            List<Secret> args = new List<Secret>() { CreateSecret(), CreateSecret() };

            Provider.SetApiResourceSecretsAsync(result, args).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(args, result.ApiSecrets);
            Assert.NotEqual("test", result.ApiSecrets.First().Type);

            args[0].Type = "test";

            Provider.ReplaceApiResourceSecretAsync(result, args[0]).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(args, result.ApiSecrets);
            Assert.Equal("test", result.ApiSecrets.First().Type);

            Provider.RemoveApiResourceSecretAsync(result, args[0]).Wait();
            args.RemoveAt(0);

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(args, result.ApiSecrets);

            Provider.ClearAllApiResourceSecretsAsync(result).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Secret>(), result.ApiSecrets);

            Assert.Throws<AggregateException>(() => Provider.SetApiResourceSecretsAsync(result, null).Wait());

            Provider.RemoveResourceAsync(apiResource).Wait();
        }
        
        [Fact]
        public void CanManageApiResourceScopes()
        {
            IResourceStore store = Fixture.GetService<IResourceStore>();

            ApiResource apiResource = CreateApiResource();

            ApiResource result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.Null(result);

            Provider.AddResourceAsync(apiResource).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Scope>(), result.Scopes);

            List<Scope> args = new List<Scope>() { CreateScope(), CreateScope() };

            Provider.SetApiResourceScopesAsync(result, args).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(args.Select(p=>p.Name), result.Scopes.Select(p => p.Name));
            Assert.NotEqual("test", result.Scopes.First().Description);

            args[0].Description = "test";

            Provider.ReplaceApiResourceScopeAsync(result, args[0]).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(args.Select(p => p.Name), result.Scopes.Select(p => p.Name));
            Assert.Equal("test", result.Scopes.First().Description);

            Provider.RemoveApiResourceScopeAsync(result, args[0]).Wait();
            args.RemoveAt(0);

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(args.Select(p => p.Name), result.Scopes.Select(p => p.Name));

            Provider.ClearAllApiResourceScopesAsync(result).Wait();

            result = store.FindApiResourceAsync(apiResource.Name).Result;
            Assert.NotNull(result);
            Assert.Equal(new List<Scope>(), result.Scopes);

            Assert.Throws<AggregateException>(() => Provider.SetApiResourceScopesAsync(result, null).Wait());

            Provider.RemoveResourceAsync(apiResource).Wait();
        }

        [Fact]
        public void ResourceStore()
        {
            IResourceStore store = Fixture.GetService<IResourceStore>();

            ApiResource res1 = CreateApiResource();
            ApiResource res2 = CreateApiResource();
            IdentityResource res3 = CreateIdentityResource();
            IdentityResource res4 = CreateIdentityResource();
            IdentityResource res5 = CreateIdentityResource();

            Provider.AddResourceAsync(res1).Wait();
            Provider.AddResourceAsync(res2).Wait();
            Provider.AddResourceAsync(res3).Wait();
            Provider.AddResourceAsync(res4).Wait();
            Provider.AddResourceAsync(res5).Wait();

            List<Scope> argScope = new List<Scope>() { new Scope() { Name = "aaa" }, new Scope() { Name = "bbb" }, new Scope() { Name = "ccc" } };
            List<Secret> argSec = new List<Secret>() { CreateSecret(), CreateSecret() };
            Provider.SetApiResourceScopesAsync(res1, argScope).Wait();
            Provider.SetApiResourceSecretsAsync(res1, argSec).Wait();
            Provider.SetApiResourceScopesAsync(res2, new List<Scope>() { new Scope() { Name = "aaa" }, new Scope() { Name = "bbb" } }).Wait();

            List<IdentityResource> idres = store.FindIdentityResourcesByScopeAsync(new string[] { res4.Name, res5.Name }).Result?.ToList();
            Assert.NotNull(idres);
            Assert.Equal(2, idres.Count);

            List<ApiResource> apires = store.FindApiResourcesByScopeAsync(new string[] { "bbb", "ccc" }).Result?.ToList();
            Assert.NotNull(apires);
            Assert.Equal(2, apires.Count);
            Assert.Equal(argScope.Select(p => p.Name), apires[0].Scopes.Select(p => p.Name));
            Assert.Equal(argSec, apires[0].ApiSecrets);

            Resources res = store.GetAllResourcesAsync().Result;
            Assert.NotNull(res);
            Assert.Equal(2, res.ApiResources.Count);
            Assert.Equal(3, res.IdentityResources.Count);
            Assert.Equal(argScope.Select(p => p.Name), res.ApiResources.First().Scopes.Select(p => p.Name));
            Assert.Equal(argSec, res.ApiResources.First().ApiSecrets);
        }

        [Fact]
        public void CorsPolicy()
        {
            IClientStore store = Fixture.GetService<IClientStore>();

            Client client = CreateClient();

            client.AllowedCorsOrigins = new List<string>() { "aaa","bbb" };

            Client result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.Null(result);

            Provider.AddClientAsync(client).Wait();
            
            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);

            ICorsPolicyService cors = Fixture.GetService<ICorsPolicyService>();

            Assert.False(cors.IsOriginAllowedAsync("ccc").Result);
            Assert.True(cors.IsOriginAllowedAsync("aaa").Result);
        }

        [Fact]
        public void TokenCleanup()
        {
            IPersistedGrantStore store = Fixture.GetService<IPersistedGrantStore>();
            IHostedService svc = Fixture.GetService<IHostedService>();
            svc.StartAsync(default(CancellationToken)).Wait();

            PersistedGrant grant1 = CreateGrant("aaa", "t1");
            PersistedGrant grant2 = CreateGrant("aaa");

            grant1.Expiration = DateTime.Now.Add(TimeSpan.FromSeconds(15));
            grant2.Expiration = DateTime.Now.Add(TimeSpan.FromSeconds(25));

            store.StoreAsync(grant1).Wait();
            store.StoreAsync(grant2).Wait();

            List<PersistedGrant> results = store.GetAllAsync("aaa").Result?.ToList();
            Assert.Equal(2, results.Count);

            Task.Delay(20000).Wait();

            results = store.GetAllAsync("aaa").Result?.ToList();
            Assert.Equal(1, results.Count);

            Task.Delay(10000).Wait();

            results = store.GetAllAsync("aaa").Result?.ToList();
            Assert.Equal(0, results.Count);
        }
    }
}
