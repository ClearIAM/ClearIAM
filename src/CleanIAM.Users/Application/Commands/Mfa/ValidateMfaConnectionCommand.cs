using System.Net;
using Marten;
using CleanIAM.SharedKernel.Application.Interfaces.Utils;
using CleanIAM.SharedKernel.Infrastructure.Utils;
using CleanIAM.Users.Core;
using CleanIAM.Users.Core.Events.Mfa;
using Wolverine;

namespace CleanIAM.Users.Application.Commands.Mfa;

/// <summary>
/// Command to validate and enable MFA connection for a user.
/// </summary>
/// <param name="Id">Id of the user</param>
/// <param name="Totp">Mfa code to validate</param>
/// <param name="EnableMfa">Flag to enable MFA</param>
public record ValidateMfaConnectionCommand(Guid Id, string Totp, bool EnableMfa);

public class ValidateMfaConnectionCommandHandler
{
    public static async Task<Result<User>> LoadAsync(ValidateMfaConnectionCommand command, IQuerySession session,
        CancellationToken cancellationToken)
    {
        var user = await session.LoadAsync<User>(command.Id, cancellationToken);

        if (user is null)
            return Result.Error("User not found", HttpStatusCode.NotFound);

        if (string.IsNullOrEmpty(user.MfaConfig.TotpSecretKey))
            return Result.Error("Totp secret not configured, generate new QR code before you continue.",
                HttpStatusCode.NotFound);

        return Result.Ok(user);
    }

    public static async Task<Result> HandleAsync(ValidateMfaConnectionCommand command,
        Result<User> loadResult, IDocumentSession session, ITotpValidator totpValidator, IMessageBus bus,
        CancellationToken cancellationToken, ILogger<ValidateMfaConnectionCommandHandler> logger)

    {
        if (loadResult.IsError())
            return Result.From(loadResult);
        var user = loadResult.Value;

        // Validate the totp code
        var validationRes = totpValidator.ValidateTotp(command.Totp, user.MfaConfig.TotpSecretKey);
        if (validationRes.IsError())
            return Result.Error("Invalid MFA code", HttpStatusCode.BadRequest);

        // Configure MFA for the user
        user.MfaConfig.IsMfaConfigured = true;
        user.IsMFAEnabled = command.EnableMfa;
        session.Update(user);
        await session.SaveChangesAsync(cancellationToken);

        // Log the mfa configuration
        logger.LogInformation("User {Id} mfa configuration validated", user.Id);

        // Publish the MfaConfigured event
        var mfaConfiguredEvent = new MfaConfiguredForUser(user.Id, user.MfaConfig.TotpSecretKey, command.EnableMfa);
        await bus.PublishAsync(mfaConfiguredEvent);
        return Result.Ok();
    }
}