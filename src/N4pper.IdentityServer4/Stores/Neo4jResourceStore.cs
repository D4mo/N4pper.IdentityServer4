using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using N4pper.IdentityServer4.Model;
using N4pper.QueryUtils;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N4pper.IdentityServer4.Stores
{
    /// <summary>
    /// Implementation of IResourceStore thats uses EF.
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IResourceStore" />
    public class Neo4jResourceStore : IResourceStore
    {
        private readonly IdentityServerDriverProvider _context;
        private readonly ILogger<Neo4jResourceStore> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Neo4jResourceStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public Neo4jResourceStore(IdentityServerDriverProvider context, ILogger<Neo4jResourceStore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        /// <summary>
        /// Finds the API resource by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node a = new Node(type: typeof(ApiResource));
                Node secret = new Node(type: typeof(Secret));
                Node scope = new Node(type: typeof(Scope));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                ApiResource result = await session.AsAsync(s =>
                s.ExecuteQuery<ApiResource, IEnumerable<Secret>, IEnumerable<Scope>>(
                $"MATCH (c{a.Labels} {{{nameof(ApiResource.Name)}:${nameof(name)}}}) " +
                $"OPTIONAL MATCH (c)-{rel}->(s{secret.Labels}) " +
                $"OPTIONAL MATCH (c)-{rel}->(sc{scope.Labels}) " +
                $"WITH c,s,sc ORDER BY id(c),id(s),id(sc) " +
                $"WITH c, {{s:collect(distinct s), sc:collect(distinct sc)}} AS val " +
                $"WITH c, val.s AS s, val.sc AS sc " +
                $"RETURN c, s, sc",
                (api, secrets, scopes) =>
                {
                    api.ApiSecrets = secrets?.Select(p => p as Secret)?.ToList();
                    api.Scopes = scopes?.Select(p => p as Scope)?.ToList();
                    return api;
                },
                new { name })
                .FirstOrDefault());

                _logger.LogDebug("Found {api} API resource in database: {found}", name, result!=null);

                return result;
            }
        }

        /// <summary>
        /// Gets API resources by scope name.
        /// </summary>
        /// <param name="scopeNames"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var names = scopeNames.ToArray();
            
            using (ISession session = _context.GetDriver().Session())
            {
                Node a = new Node(type: typeof(ApiResource));
                Node secret = new Node(type: typeof(Secret));
                Node scope = new Node(type: typeof(Scope));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                List<ApiResource> result = await session.AsAsync(s =>
                s.ExecuteQuery<ApiResource, IEnumerable<Secret>, IEnumerable<Scope>>(
                $"MATCH (c{a.Labels}) " +
                $"OPTIONAL MATCH (c)-{rel}->(s{secret.Labels}) " +
                $"OPTIONAL MATCH (c)-{rel}->(sc{scope.Labels}) " +
                $"WITH c,s,sc ORDER BY id(c),id(s),id(sc) " +
                $"WITH c, {{s:collect(distinct s), sc:collect(distinct sc)}} AS val " +
                $"WITH c, val.s AS s, val.sc AS sc " +
                $"WHERE sc IS NOT NULL AND FILTER(x in sc where x.{nameof(Scope.Name)} in ${nameof(names)}) <> [] " +
                $"WITH c,s,sc ORDER BY id(c) " +
                $"RETURN c, s, sc",
                (api, secrets, scopes) =>
                {
                    api.ApiSecrets = secrets?.Select(p => p as Secret)?.ToList();
                    api.Scopes = scopes?.Select(p => p as Scope)?.ToList();
                    return api;
                },
                new { names })
                .ToList());

                return result;
            }
        }

        /// <summary>
        /// Gets identity resources by scope name.
        /// </summary>
        /// <param name="scopeNames"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var names = scopeNames.ToArray();

            using (ISession session = _context.GetDriver().Session())
            {
                Node a = new Node(type: typeof(IdentityResource));

                List<IdentityResource> result = await session.AsAsync(s =>
                s.ExecuteQuery<IdentityResource>(
                $"MATCH (c{a.Labels}) " +
                $"WHERE c.{nameof(IdentityResource.Name)} IN ${nameof(names)} " +
                $"RETURN c",
                new { names })
                .ToList());

                return result;
            }
        }

        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns></returns>
        public async Task<Resources> GetAllResourcesAsync()
        {

            using (ISession session = _context.GetDriver().Session())
            {
                Node a = new Node(type: typeof(ApiResource));
                Node secret = new Node(type: typeof(Secret));
                Node scope = new Node(type: typeof(Scope));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                List<ApiResource> apiResources = await session.AsAsync(s =>
                s.ExecuteQuery<ApiResource, IEnumerable<Secret>, IEnumerable<Scope>>(
                $"MATCH (c{a.Labels}) " +
                $"OPTIONAL MATCH (c)-{rel}->(s{secret.Labels}) " +
                $"OPTIONAL MATCH (c)-{rel}->(sc{scope.Labels}) " +
                $"WITH c,s,sc ORDER BY id(c),id(s),id(sc) " +
                $"WITH c, {{s:collect(distinct s), sc:collect(distinct sc)}} AS val " +
                $"WITH c, val.s AS s, val.sc AS sc ORDER BY id(c) " +
                $"RETURN c, s, sc",
                (api, secrets, scopes) =>
                {
                    api.ApiSecrets = secrets?.Select(p => p as Secret)?.ToList();
                    api.Scopes = scopes?.Select(p => p as Scope)?.ToList();
                    return api;
                })
                .ToList());

                Node i = new Node(type: typeof(IdentityResource));

                List<IdentityResource> identityResources = await session.AsAsync(s =>
                s.ExecuteQuery<IdentityResource>(
                $"MATCH (c{i.Labels}) " +
                $"RETURN c")
                .ToList());

                Resources result = new Resources(
                identityResources,
                apiResources);

                _logger.LogDebug("Found {scopes} as all scopes in database", result.IdentityResources.Select(x => x.Name).Union(result.ApiResources.SelectMany(x => x.Scopes).Select(x => x.Name)));

                return result;
            }
        }
    }
}
