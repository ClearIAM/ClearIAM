using CleanIAM.Tenants.Core;
using Marten;

namespace CleanIAM.Tenants.Application.Queries;

public record GetAllTenantsQuery;

public class GetAllTenantsQueryHandler
{
    public static async Task<IEnumerable<Tenant>> HandleAsync(GetAllTenantsQuery query, IQuerySession session,
        CancellationToken cancellationToken)
    {
        return await session.Query<Tenant>().Where(t => t.AnyTenant()).ToListAsync(cancellationToken);
    }
}