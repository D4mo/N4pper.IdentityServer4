using IdentityServer4.Models;
using N4pper.IdentityServer4.Model;
using N4pper.QueryUtils;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OMnG;
using System.Linq;
using System.Security.Claims;

namespace N4pper.IdentityServer4
{
    public static class IdentityServerDriverProviderExtensions
    {
        public static async Task AddClientAsync(this IdentityServerDriverProvider ext, Client client)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            client = client ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));

                Neo4jClient newClient = await session.AsAsync(s=>
                s.ExecuteQuery<Neo4jClient>($"CREATE (p{n.Labels}) SET p+=${nameof(client)}, p.{nameof(IGraphEntity.EntityId)}=id(p) RETURN p", 
                    new { client = client.ExludeProperties(p=>new { p.Properties, p.Claims, p.ClientSecrets }) }).FirstOrDefault());

                if (client is Neo4jClient)
                    (client as Neo4jClient).EntityId = newClient.EntityId;
            }
        }
        public static async Task UpdateClientAsync(this IdentityServerDriverProvider ext, Client client)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            client = client ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(client)}.{nameof(Client.ClientId)}}}) " +
                    $"SET n+=${nameof(client)}",
                    new { client = client.ExludeProperties(p => new { p.Properties, p.Claims, p.ClientSecrets }) });
            }
        }
        public static async Task RemoveClientAsync(this IdentityServerDriverProvider ext, Client client)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jProperty));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}}) " +
                    $"OPTIONAL MATCH (n)-{rel}->(p) " +
                    $"DETACH DELETE p, n",
                    new { clientId });
            }
        }

        public static async Task SetClientPropsAsync(this IdentityServerDriverProvider ext, Client client, Dictionary<string, string> properties)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            List<Neo4jProperty> props = properties?.Select(p=>new Neo4jProperty() { Name = p.Key, Value = p.Value }).ToList() ?? new List<Neo4jProperty>();
            if (props.Count == 0)
                throw new ArgumentException("No item to set");

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jProperty));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}}) " +
                    $"OPTIONAL MATCH (n)-{rel}->(p{p.Labels}) " +
                    $"WITH n, collect(p) AS olds " +
                    $"UNWIND ${nameof(props)} AS row " +
                    $"CREATE (n)-{rel}->(q{p.Labels}) " +
                    $"SET q+=row,q.{nameof(IGraphEntity.EntityId)}=id(q) " +
                    $"WITH olds " +
                    $"UNWIND olds AS old " +
                    $"DETACH DELETE old",
                    new { clientId, props });
            }
        }
        public static async Task ReplaceClientPropAsync(this IdentityServerDriverProvider ext, Client client, string name, string value)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            if(string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            value = value ?? throw new ArgumentNullException(nameof(value));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jProperty));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jProperty.Name)}:${nameof(name)}}}) " +
                    $"SET p.{nameof(Neo4jProperty.Value)}=${nameof(value)}",
                    new { clientId, name, value });
            }
        }
        public static async Task RemoveClientPropAsync(this IdentityServerDriverProvider ext, Client client, string name)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jProperty));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jProperty.Name)}:${nameof(name)}}}) " +
                    $"DETACH DELETE p",
                    new { clientId, name });
            }
        }
        public static async Task ClearAllClientPropsAsync(this IdentityServerDriverProvider ext, Client client)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jProperty));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels}) " +
                    $"DETACH DELETE p",
                    new { clientId });
            }
        }

        public static async Task SetClientClaimsAsync(this IdentityServerDriverProvider ext, Client client, IEnumerable<Claim> claims)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            List<Neo4jClaim> newClaims = claims?.Select(p => { var t = new Neo4jClaim(); t.InitializeFromClaim(p); return t; }).ToList() ?? new List<Neo4jClaim>();
            if (newClaims.Count == 0)
                throw new ArgumentException("No item to set");

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}}) " +
                    $"OPTIONAL MATCH (n)-{rel}->(p{p.Labels}) " +
                    $"WITH n, collect(p) AS olds " +
                    $"UNWIND ${nameof(newClaims)} AS row " +
                    $"CREATE (n)-{rel}->(q{p.Labels}) " +
                    $"SET q+=row,q.{nameof(IGraphEntity.EntityId)}=id(q) " +
                    $"WITH olds " +
                    $"UNWIND olds AS old " +
                    $"DETACH DELETE old",
                    new { clientId, newClaims });
            }
        }
        public static async Task ReplaceClientClaimAsync(this IdentityServerDriverProvider ext, Client client, string type, string value)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));
            value = value ?? throw new ArgumentNullException(nameof(value));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jClaim.ClaimType)}:${nameof(type)}}}) " +
                    $"SET p.{nameof(Neo4jClaim.ClaimValue)}=${nameof(value)}",
                    new { clientId, type, value });
            }
        }
        public static async Task RemoveClientClaimAsync(this IdentityServerDriverProvider ext, Client client, string type)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jClaim.ClaimType)}:${nameof(type)}}}) " +
                    $"DETACH DELETE p",
                    new { clientId, type });
            }
        }
        public static async Task ClearAllClientClaimsAsync(this IdentityServerDriverProvider ext, Client client)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jClaim));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels}) " +
                    $"DETACH DELETE p",
                    new { clientId });
            }
        }

        public static async Task SetClientSecretsAsync(this IdentityServerDriverProvider ext, Client client, IEnumerable<Secret> secrets)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            List<Secret> newSecrets = secrets?.ToList() ?? new List<Secret>();
            if (newSecrets.Count == 0)
                throw new ArgumentException("No item to set");

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}}) " +
                    $"OPTIONAL MATCH (n)-{rel}->(p{p.Labels}) " +
                    $"WITH n, collect(p) AS olds " +
                    $"UNWIND ${nameof(newSecrets)} AS row " +
                    $"CREATE (n)-{rel}->(q{p.Labels}) " +
                    $"SET q+=row,q.{nameof(IGraphEntity.EntityId)}=id(q) " +
                    $"WITH olds " +
                    $"UNWIND olds AS old " +
                    $"DETACH DELETE old",
                    new { clientId, newSecrets });
            }
        }
        public static async Task ReplaceClientSecretAsync(this IdentityServerDriverProvider ext, Client client, Secret secret)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            secret = secret ?? throw new ArgumentNullException(nameof(secret));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jSecret.Description)}:${nameof(secret)}.{nameof(secret.Description)}}}) " +
                    $"SET p+=${nameof(secret)}",
                    new { clientId, secret });
            }
        }
        public static async Task RemoveClientSecretAsync(this IdentityServerDriverProvider ext, Client client, Secret secret)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            secret = secret ?? throw new ArgumentNullException(nameof(secret));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));
            string description = secret.Description;

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jSecret.Description)}:${nameof(description)}}}) " +
                    $"DETACH DELETE p",
                    new { clientId, description });
            }
        }
        public static async Task ClearAllClientSecretsAsync(this IdentityServerDriverProvider ext, Client client)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string clientId = client?.ClientId ?? throw new ArgumentNullException(nameof(client));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jClient));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels}) " +
                    $"DETACH DELETE p",
                    new { clientId });
            }
        }

        public static async Task AddPersistedGrantAsync(this IdentityServerDriverProvider ext, PersistedGrant grant)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            grant = grant ?? throw new ArgumentNullException(nameof(grant));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jPersistedGrant));

                Neo4jPersistedGrant newGrant = await session.AsAsync(s =>
                s.ExecuteQuery<Neo4jPersistedGrant>($"CREATE (p{n.Labels}) SET p+=${nameof(grant)}, p.{nameof(IGraphEntity.EntityId)}=id(p) RETURN p",
                    new { grant }).FirstOrDefault());

                if (grant is IGraphEntity)
                    (grant as IGraphEntity).EntityId = newGrant.EntityId;
            }
        }
        public static async Task UpdatePersistedGrantAsync(this IdentityServerDriverProvider ext, PersistedGrant grant)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            grant = grant ?? throw new ArgumentNullException(nameof(grant));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(PersistedGrant));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(PersistedGrant.Key)}:${nameof(grant)}.{nameof(PersistedGrant.Key)}}}) " +
                    $"SET n+=${nameof(grant)}",
                    new { grant });
            }
        }
        public static async Task RemovePersistedGrantAsync(this IdentityServerDriverProvider ext, PersistedGrant grant)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string key = grant?.Key ?? throw new ArgumentNullException(nameof(grant));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(PersistedGrant));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(PersistedGrant.Key)}:${nameof(key)}}})" +
                    $"DETACH DELETE n",
                    new { key });
            }
        }
        
        public static async Task AddResourceAsync(this IdentityServerDriverProvider ext, Resource resource)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            resource = resource ?? throw new ArgumentNullException(nameof(resource));

            Type resType = resource is IdentityResource ? typeof(Neo4jIdentityResource) : typeof(Neo4jApiResource);
            IDictionary<string, object> resArg = resource is IdentityResource ? resource.ToPropDictionary() : (resource as ApiResource).ExludeProperties(p=>new { p.Scopes, p.ApiSecrets });

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: resType);

                Resource newResource = await session.AsAsync(s =>
                s.ExecuteQuery<Resource>($"CREATE (p{n.Labels}) SET p+=${nameof(resource)}, p.{nameof(IGraphEntity.EntityId)}=id(p) RETURN p",
                    new { resource = resArg }).FirstOrDefault());

                if (resource is IGraphEntity)
                    (resource as IGraphEntity).EntityId = (newResource as IGraphEntity)?.EntityId;
            }
        }
        public static async Task UpdateResourceAsync(this IdentityServerDriverProvider ext, Resource resource)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            resource = resource ?? throw new ArgumentNullException(nameof(resource));

            Type resType = resource is IdentityResource ? typeof(Neo4jIdentityResource) : typeof(Neo4jApiResource);
            IDictionary<string, object> resArg = resource is IdentityResource ? resource.ToPropDictionary() : (resource as ApiResource).ExludeProperties(p => new { p.Scopes, p.ApiSecrets });

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: resType);

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(resource)}.{nameof(Resource.Name)}}}) " +
                    $"SET n+=${nameof(resource)}",
                    new { resource = resArg });
            }
        }
        public static async Task RemoveResourceAsync(this IdentityServerDriverProvider ext, Resource resource)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));

            Type resType = resource is IdentityResource ? typeof(Neo4jIdentityResource) : typeof(Neo4jApiResource);

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: resType);
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}}) " +
                    $"OPTIONAL MATCH (n)-{rel}->(p) " +
                    $"DETACH DELETE p, n",
                    new { name });
            }
        }
        
        public static async Task SetApiResourceSecretsAsync(this IdentityServerDriverProvider ext, ApiResource resource, IEnumerable<Secret> secrets)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));

            List<Secret> newSecrets = secrets?.ToList() ?? new List<Secret>();
            if (newSecrets.Count == 0)
                throw new ArgumentException("No item to set");

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}}) " +
                    $"OPTIONAL MATCH (n)-{rel}->(p{p.Labels}) " +
                    $"WITH n, collect(p) AS olds " +
                    $"UNWIND ${nameof(newSecrets)} AS row " +
                    $"CREATE (n)-{rel}->(q{p.Labels}) " +
                    $"SET q+=row,q.{nameof(IGraphEntity.EntityId)}=id(q) " +
                    $"WITH olds " +
                    $"UNWIND olds AS old " +
                    $"DETACH DELETE old",
                    new { name, newSecrets });
            }
        }
        public static async Task ReplaceApiResourceSecretAsync(this IdentityServerDriverProvider ext, ApiResource resource, Secret secret)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            secret = secret ?? throw new ArgumentNullException(nameof(secret));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jSecret.Description)}:${nameof(secret)}.{nameof(secret.Description)}}}) " +
                    $"SET p+=${nameof(secret)}",
                    new { name, secret });
            }
        }
        public static async Task RemoveApiResourceSecretAsync(this IdentityServerDriverProvider ext, ApiResource resource, Secret secret)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            secret = secret ?? throw new ArgumentNullException(nameof(secret));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));
            string description = secret.Description;

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jSecret.Description)}:${nameof(description)}}}) " +
                    $"DETACH DELETE p",
                    new { name, description });
            }
        }
        public static async Task ClearAllApiResourceSecretsAsync(this IdentityServerDriverProvider ext, ApiResource resource)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jSecret));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels}) " +
                    $"DETACH DELETE p",
                    new { name });
            }
        }


        public static async Task SetApiResourceScopesAsync(this IdentityServerDriverProvider ext, ApiResource resource, IEnumerable<Scope> scopes)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));

            List<Scope> newScopes = scopes?.ToList() ?? new List<Scope>();
            if (newScopes.Count == 0)
                throw new ArgumentException("No item to set");

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jScope));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}}) " +
                    $"OPTIONAL MATCH (n)-{rel}->(p{p.Labels}) " +
                    $"WITH n, collect(p) AS olds " +
                    $"UNWIND ${nameof(newScopes)} AS row " +
                    $"CREATE (n)-{rel}->(q{p.Labels}) " +
                    $"SET q+=row,q.{nameof(IGraphEntity.EntityId)}=id(q) " +
                    $"WITH olds " +
                    $"UNWIND olds AS old " +
                    $"DETACH DELETE old",
                    new { name, newScopes });
            }
        }
        public static async Task ReplaceApiResourceScopeAsync(this IdentityServerDriverProvider ext, ApiResource resource, Scope scope)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            scope = scope ?? throw new ArgumentNullException(nameof(scope));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jScope));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jScope.Name)}:${nameof(scope)}.{nameof(scope.Name)}}}) " +
                    $"SET p+=${nameof(scope)}",
                    new { name, scope });
            }
        }
        public static async Task RemoveApiResourceScopeAsync(this IdentityServerDriverProvider ext, ApiResource resource, Scope scope)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));
            scope = scope ?? throw new ArgumentNullException(nameof(scope));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));
            string scopeName = scope.Name;

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jScope));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels} {{{nameof(Neo4jScope.Name)}:${nameof(scopeName)}}}) " +
                    $"DETACH DELETE p",
                    new { name, scopeName });
            }
        }
        public static async Task ClearAllApiResourceScopesAsync(this IdentityServerDriverProvider ext, ApiResource resource)
        {
            ext = ext ?? throw new ArgumentNullException(nameof(ext));

            string name = resource?.Name ?? throw new ArgumentNullException(nameof(resource));

            using (ISession session = ext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Neo4jApiResource));
                Node p = new Node(type: typeof(Neo4jScope));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(Resource.Name)}:${nameof(name)}}})" +
                    $"-{rel}->" +
                    $"(p{p.Labels}) " +
                    $"DETACH DELETE p",
                    new { name });
            }
        }
    }
}
