using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using N4pper;
using N4pper.IdentityServer4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
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

            result.ClientName = "test2";
            result.RedirectUris.Add("ccc");
            result.RedirectUris.Remove("bbb");

            Provider.UpdateClientAsync(result).Wait();

            result = store.FindClientByIdAsync(client.ClientId).Result;
            Assert.NotNull(result);
            Assert.Equal(new string[] { "aaa", "ccc" }, result.RedirectUris);
            Assert.Equal("test2", result.ClientName);

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
    }
}
