using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using N4pper.QueryUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N4pper.IdentityServer4.Services
{
    public class Neo4jCorsPolicyService : ICorsPolicyService
    {
        private readonly IHttpContextAccessor _context;
        private readonly ILogger<Neo4jCorsPolicyService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsPolicyService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public Neo4jCorsPolicyService(IHttpContextAccessor context, ILogger<Neo4jCorsPolicyService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        /// <summary>
        /// Determines whether origin is allowed.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <returns></returns>
        public async Task<bool> IsOriginAllowedAsync(string origin)
        {
            // doing this here and not in the ctor because: https://github.com/aspnet/CORS/issues/105
            var dbContext = _context.HttpContext.RequestServices.GetRequiredService<IdentityServerDriverProvider>();

            using (Neo4j.Driver.V1.ISession session = dbContext.GetDriver().Session())
            {
                Node n = new Node(type: typeof(Client));

                int count = await session.AsAsync(s=>
                s.ExecuteQuery<Client>(
                    $"MATCH (p{n.Labels}) " +
                    $"UNWIND p.{nameof(Client.AllowedCorsOrigins)} AS row " +
                    $"WITH LOWER(row) AS row " +
                    $"WHERE row=LOWER($origin) " +
                    $"RETURN row",
                    new { origin }).Count());

                var isAllowed = count>0;

                _logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

                return isAllowed;
            }
        }
    }
}
