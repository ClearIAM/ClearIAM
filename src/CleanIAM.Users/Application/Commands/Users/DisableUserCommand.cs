using System.Net;
using Mapster;
using Marten;
using CleanIAM.SharedKernel.Infrastructure.Utils;
using CleanIAM.Users.Core;
using CleanIAM.Users.Core.Events.Users;
using Wolverine;

namespace CleanIAM.Users.Application.Commands.Users;

/// <summary>
/// Represents a command to disable a user within the system.
/// </summary>
/// <param name="Id">Id of the user to be disabled.</param>
public record DisableUserCommand(Guid Id);

public class DisableUserCommandHandler
{
    public static async Task<Result<User>> LoadAsync(DisableUserCommand command, IQuerySession session)
    {
        var user = await session.LoadAsync<User>(command.Id);
        if (user == null)
            return Result.Error("User not found", HttpStatusCode.NotFound);

        return Result.Ok(user);
    }

    public static async Task<Result<UserDisabled>> HandleAsync(DisableUserCommand command, Result<User> loadResult,
        IMessageBus bus, IDocumentSession session, ILogger<DisableUserCommandHandler> logger)
    {
        if (loadResult.IsError())
            return Result.From(loadResult);
        var user = loadResult.Value;

        // Disable user
        user.IsDisabled = true;
        session.Update(user);
        await session.SaveChangesAsync();

        // Log the user disable action
        logger.LogInformation("User {Id} disabled", user.Id);

        // Publish user disabled event
        var userDisabledEvent = user.Adapt<UserDisabled>();
        await bus.PublishAsync(userDisabledEvent);
        return Result.Ok(userDisabledEvent);
    }
}