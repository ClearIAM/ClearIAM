using CleanIAM.Events.Core.Events.Tenants;
using CleanIAM.Users.Core;
using Marten;

namespace CleanIAM.Users.Application.EventHandlers;

/// <summary>
/// Handles the TenantUpdated event generated by CleanIAM.Tenants project and updates the corresponding users in the database.
/// </summary>
public class TenantUpdatedEventHandler
{
    public static async Task Handle(TenantUpdated tenantUpdated, IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // Query all affected users
        var users = await session.Query<User>()
            .Where(user => user.TenantId == tenantUpdated.Id && user.AnyTenant())
            .ToListAsync(cancellationToken);

        // Update users
        foreach (var user in users)
            user.TenantName = tenantUpdated.Name;

        session.Update(users.ToArray());
        await session.SaveChangesAsync(cancellationToken);
    }
}