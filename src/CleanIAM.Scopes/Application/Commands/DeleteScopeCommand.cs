using CleanIAM.Scopes.Core.Events;
using OpenIddict.Core;
using CleanIAM.SharedKernel.Infrastructure.Utils;
using Wolverine;
using OpenIddictScope = OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreScope<System.Guid>;

namespace CleanIAM.Scopes.Application.Commands;

/// <summary>
/// Command to delete existing scope.
/// </summary>
/// <param name="Name">Name of the scope to delete</param>
public record DeleteScopeCommand(string Name);

public class DeleteScopeCommandHandler
{
    public static async Task<Result<OpenIddictScope>> LoadAsync(DeleteScopeCommand command,
        OpenIddictScopeManager<OpenIddictScope> scopeManager, CancellationToken cancellationToken)
    {
        // Check if the scope with the given name isn't default scope
        if (ScopesConstants.DefaultScopeNames.Contains(command.Name))
            return Result.Error("Default scopes cannot be deleted", StatusCodes.Status400BadRequest);

        // Check if the scope with the given name does exist
        var scope = await scopeManager.FindByNameAsync(command.Name, cancellationToken);
        if (scope is null)
            return Result.Error("Scope not found", StatusCodes.Status404NotFound);

        return Result.Ok(scope);
    }

    public static async Task<Result<ScopeDeleted>> HandleAsync(DeleteScopeCommand command,
        Result<OpenIddictScope> loadResult, OpenIddictScopeManager<OpenIddictScope> scopeManager, IMessageBus bus,
        CancellationToken cancellationToken, ILogger<DeleteScopeCommandHandler> logger)
    {
        if (loadResult.IsError())
            return Result.From(loadResult);
        var scope = loadResult.Value;

        try
        {
            await scopeManager.DeleteAsync(scope, cancellationToken);
        }
        catch
        {
            return Result.Error("Error deleting scope", StatusCodes.Status500InternalServerError);
        }

        // Log the deletion
        logger.LogInformation("Scope {Name} deleted successfully", command.Name);

        // Publish the ScopeDeleted event and return
        var scopeDeletedEvent = new ScopeDeleted(command.Name);
        await bus.PublishAsync(scopeDeletedEvent);
        return Result.Ok(scopeDeletedEvent);
    }
}