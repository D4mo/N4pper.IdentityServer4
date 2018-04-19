using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using N4pper.IdentityServer4.Model;
using N4pper.QueryUtils;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace N4pper.IdentityServer4.Stores
{
    /// <summary>
    /// Implementation of IClientStore thats uses EF.
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IClientStore" />
    public class Neo4jClientStore : IClientStore
    {
        private readonly IdentityServerDriverProvider _context;
        private readonly ILogger<Neo4jClientStore> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Neo4jClientStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public Neo4jClientStore(IdentityServerDriverProvider context, ILogger<Neo4jClientStore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        /// <summary>
        /// Finds a client by id
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <returns>
        /// The client
        /// </returns>
        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            using (ISession session = _context.GetDriver().Session())
            {
                Node cli = new Node(type: typeof(Client));
                Node prop = new Node(type: typeof(Neo4jProperty));
                Node secret = new Node(type: typeof(Secret));
                Node claim = new Node(type: typeof(Neo4jClaim));
                Rel rel = new Rel(type : typeof(Relationships.Has));

                Client result = await session.AsAsync(s=>
                s.ExecuteQuery<Neo4jClient, IEnumerable<Neo4jProperty>, IEnumerable<Neo4jSecret>, IEnumerable<Neo4jClaim>>(
                $"MATCH (c{cli.Labels} {{{nameof(Client.ClientId)}:${nameof(clientId)}}}) " +
                $"OPTIONAL MATCH (c)-{rel}->(p{prop.Labels}) " +
                $"OPTIONAL MATCH (c)-{rel}->(s{secret.Labels}) " +
                $"OPTIONAL MATCH (c)-{rel}->(cl{claim.Labels}) " +
                $"WITH c, p, s, cl ORDER BY id(c), id(p), id(s), id(cl) " +
                $"WITH c, {{props: collect(distinct p), secs:collect(distinct s), cls:collect(distinct cl)}} AS val " +
                $"WITH c, val.props AS p, val.secs AS s, val.cls AS cl ORDER BY id(c) " +
                $"RETURN c, p, s, cl", 
                (client, props, secrets, claims)=> 
                {
                    client.Properties = props?.ToDictionary(p=>p.Name,p=>p.Value);
                    client.Claims = claims?.Select(p=>p.ToClaim())?.ToList();
                    client.ClientSecrets = secrets?.Select(p=>p as Secret)?.ToList();
                    return client;
                }, 
                new { clientId })
                .FirstOrDefault());

                _logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, result != null);

                return result;
            }
        }
    }
}
