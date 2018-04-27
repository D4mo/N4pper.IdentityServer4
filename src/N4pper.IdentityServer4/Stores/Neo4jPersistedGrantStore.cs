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
    /// Implementation of IPersistedGrantStore thats uses EF.
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IPersistedGrantStore" />
    public class Neo4jPersistedGrantStore : IPersistedGrantStore
    {
        private readonly IdentityServerDriverProvider _context;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Neo4jPersistedGrantStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        public Neo4jPersistedGrantStore(IdentityServerDriverProvider context, ILogger<Neo4jPersistedGrantStore> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Stores the asynchronous.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public async Task StoreAsync(PersistedGrant token)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node cli = new Node(type: typeof(Client));
                Node n = new Node(type: typeof(PersistedGrant));
                Rel rel = new Rel(type:typeof(Relationships.Has));

                IResultSummary summary = await (await session.RunAsync(
                    $"MATCH (c{cli.Labels} {{{nameof(Client.ClientId)}:$value.{nameof(token.ClientId)}}}) " +
                    $"MERGE (c)-{rel}->(n{n.Labels} {{{nameof(PersistedGrant.Key)}:$value.{nameof(token.Key)}}}) " +
                    $"ON CREATE SET n+=$value, n.{nameof(IGraphEntity.EntityId)}=id(n), n :{typeof(Neo4jPersistedGrant).Name} " +
                    $"ON MATCH SET n+=$value, n.{nameof(IGraphEntity.EntityId)}=id(n) ", new { value = token })).SummaryAsync();

                if (!summary.Counters.ContainsUpdates && summary.Counters.NodesCreated == 0)
                    _logger.LogWarning("No node have been created or updated.");
            }
        }

        /// <summary>
        /// Gets the grant.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public async Task<PersistedGrant> GetAsync(string key)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(PersistedGrant));

                PersistedGrant result = await session.AsAsync(s=>
                    s.ExecuteQuery<PersistedGrant>($"MATCH (n{n.Labels} {{{nameof(PersistedGrant.Key)}:${nameof(key)}}}) " +
                    $"RETURN n"
                    , new { key }).FirstOrDefault());
                
                _logger.LogDebug("{persistedGrantKey} found in database: {persistedGrantKeyFound}", key, result != null);

                return result;
            }
        }

        /// <summary>
        /// Gets all grants for a given subject id.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <returns></returns>
        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(PersistedGrant));

                List<PersistedGrant> result = await session.AsAsync(s =>
                    s.ExecuteQuery<PersistedGrant>($"MATCH (n{n.Labels} {{{nameof(PersistedGrant.SubjectId)}:${nameof(subjectId)}}}) " +
                    $"RETURN n"
                    , new { subjectId }).ToList());

                _logger.LogDebug("{persistedGrantCount} persisted grants found for {subjectId}", result.Count, subjectId);

                return result;
            }
        }

        /// <summary>
        /// Removes the grant by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public async Task RemoveAsync(string key)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node n = new Node(type: typeof(PersistedGrant));

                IResultSummary summary = await (await session.RunAsync(
                    $"MATCH (n{n.Labels} {{{nameof(PersistedGrant.Key)}:${nameof(key)}}}) " +
                    $"DETACH DELETE n"
                    , new { key })).SummaryAsync();

                if (summary.Counters.NodesDeleted == 0)
                    _logger.LogDebug("no {persistedGrantKey} persisted grant found in database", key);
            }
        }

        /// <summary>
        /// Removes all grants for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns></returns>
        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node c = new Node(type: typeof(Client));
                Node n = new Node(type: typeof(PersistedGrant));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                IResultSummary summary = await (await session.RunAsync(
                    $"MATCH (c{c.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(n{n.Labels} {{{nameof(PersistedGrant.ClientId)}:${nameof(clientId)},{nameof(PersistedGrant.SubjectId)}:${nameof(subjectId)}}})" +
                    $"DETACH DELETE n"
                    , new { clientId, subjectId })).SummaryAsync();
            }
        }

        /// <summary>
        /// Removes all grants of a give type for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node c = new Node(type: typeof(Client));
                Node n = new Node(type: typeof(PersistedGrant));
                Rel rel = new Rel(type: typeof(Relationships.Has));

                IResultSummary summary = await(await session.RunAsync(
                    $"MATCH (c{c.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}})" +
                    $"-{rel}->" +
                    $"(n{n.Labels} {{{nameof(PersistedGrant.ClientId)}:${nameof(clientId)},{nameof(PersistedGrant.SubjectId)}:${nameof(subjectId)},{nameof(PersistedGrant.Type)}:${nameof(type)}}}) " +
                    $"DETACH DELETE n"
                    , new { clientId, subjectId, type })).SummaryAsync();
            }
        }
    }
}
