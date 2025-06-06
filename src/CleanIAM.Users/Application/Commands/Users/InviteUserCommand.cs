using CleanIAM.SharedKernel;
using Mapster;
using Marten;
using CleanIAM.SharedKernel.Core;
using CleanIAM.SharedKernel.Infrastructure.Utils;
using CleanIAM.Users.Core;
using CleanIAM.Users.Core.Events.Users;
using Wolverine;

namespace CleanIAM.Users.Application.Commands.Users;

/// <summary>
/// Command to invite a user.
/// </summary>
/// <param name="Id">If of invited user</param>
/// <param name="Email">Email of invited user</param>
/// <param name="FirstName">First name of invited user</param>
/// <param name="LastName">Last name of invited user</param>
/// <param name="Roles">Roles of invited user</param>
public record InviteUserCommand(Guid Id, string Email, string FirstName, string LastName, UserRole[] Roles);

public class InviteUserCommandHandler
{
    public async Task<Result<Guid>> LoadAsync(InviteUserCommand command, IQuerySession session,
        CancellationToken cancellationToken)
    {
        var user = await session.Query<User>()
            .FirstOrDefaultAsync(u => u.Email == command.Email.ToLowerInvariant(), cancellationToken);
        if (user is not null)
            return Result.Error("User already exists", StatusCodes.Status400BadRequest);

        return Result.Ok(Guid.TryParse(session.TenantId, out var tenantId)
            ? tenantId
            : SharedKernelConstants.DefaultTenantId);
    }

    public async Task<Result<UserInvited>> HandleAsync(InviteUserCommand command, Result<Guid> loadResult,
        IMessageBus bus,
        IDocumentSession session, CancellationToken cancellationToken, ILogger logger)
    {
        if (loadResult.IsError())
            return Result.From(loadResult);
        var tenantId = loadResult.Value;

        var user = command.Adapt<User>();
        user.IsInvitePending = true;
        user.Email = user.Email.ToLowerInvariant(); // Normalize email
        user.TenantId = tenantId;
        session.Store(user);
        await session.SaveChangesAsync(cancellationToken);

        // Log the user invitation
        logger.LogInformation("User {Id} invited", user.Id);

        var userInvited = user.Adapt<UserInvited>();
        await bus.PublishAsync(userInvited);
        return Result.Ok(userInvited);
    }
}