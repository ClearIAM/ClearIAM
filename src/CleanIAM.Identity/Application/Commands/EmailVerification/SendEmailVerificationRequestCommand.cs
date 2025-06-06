using System.Net;
using CleanIAM.Identity.Application.Interfaces;
using CleanIAM.Identity.Core.Events;
using CleanIAM.Identity.Core.Mails;
using CleanIAM.Identity.Core.Requests;
using CleanIAM.Identity.Core.Users;
using Mapster;
using Marten;
using CleanIAM.SharedKernel.Application.Interfaces;
using CleanIAM.SharedKernel.Infrastructure.Utils;
using CleanIAM.UrlShortener.Application.Commands;
using CleanIAM.UrlShortener.Core.Events;
using Wolverine;

namespace CleanIAM.Identity.Application.Commands.EmailVerification;

/// <summary>
/// Send email verification request.
/// </summary>
/// <param name="UserId">Id of the user the request is requested for</param>
public record SendEmailVerificationRequestCommand(Guid UserId);

/// <summary>
/// This handler loads or creates a new email verification request and sends email verification.
/// </summary>
public class SendEmailVerificationRequestCommandHandler
{
    public static async Task<Result<EmailVerificationRequest>> LoadAsync(SendEmailVerificationRequestCommand command,
        IQuerySession querySession, CancellationToken cancellationToken)
    {
        // Check if the user for a given request exists
        var user = await querySession.Query<IdentityUser>().FirstOrDefaultAsync(u => u.Id == command.UserId && u.AnyTenant(), cancellationToken);
        if (user is null)
            return Result.Error("User not found", HttpStatusCode.NotFound);

        var request = querySession.Query<EmailVerificationRequest>()
            .FirstOrDefault(x => x.UserId == user.Id);

        // If request for given user doesn't exist, create a new one
        if (request is null)
        {
            var newRequest = user.Adapt<EmailVerificationRequest>();
            newRequest.Id = Guid.NewGuid();
            newRequest.LastEmailsSendAt = DateTime.MinValue;
            newRequest.UserId = user.Id;
            return Result.Ok(newRequest);
        }

        // If the request exists, check if it wasn't sent too recently
        var timeSinceLastEmail = DateTime.UtcNow - request.LastEmailsSendAt;
        if (timeSinceLastEmail < IdentityConstants.VerificationEmailDelay)
            return Result.Error(
                $"Email verification request already send, " +
                $"you need to wait" + ((IdentityConstants.VerificationEmailDelay - timeSinceLastEmail).Minutes != 0
                    ? $" {(IdentityConstants.VerificationEmailDelay - timeSinceLastEmail).Minutes} minutes "
                    : $" {(IdentityConstants.VerificationEmailDelay - timeSinceLastEmail).Seconds} seconds ") +
                $"before you request new email.",
                HttpStatusCode.BadRequest);

        return Result.Ok(request);
    }

    public static async Task<Result<EmailVerificationRequestSent>> HandleAsync(
        SendEmailVerificationRequestCommand command,
        Result<EmailVerificationRequest> result, IEmailService emailService, IAppConfiguration configuration,
        IDocumentSession documentSession, IMessageBus bus, ILogger<SendEmailVerificationRequestCommandHandler> logger)
    {
        if (result.IsError())
            return Result.From(result);
        var request = result.Value;

        // Build the email verification url
        var verificationUrl = $"{configuration.IdentityBaseUrl}/email-verification/{request.Id.ToString()}";

        // Shorten the url if shortening is enabled
        //TODO: Anti-corruption layer
        if (configuration.UseUrlShortener)
        {
            var shortenUrlCommand = new ShortenUrlCommand(verificationUrl);
            var shortingRes = await bus.InvokeAsync<Result<UrlShortened>>(shortenUrlCommand);
            if (shortingRes.IsError())
                return Result.From(shortingRes);
            verificationUrl = shortingRes.Value.ShortenedUrl;
        }

        // Send verification email
        var res = await emailService.SendVerificationEmailAsync(request.Adapt<EmailRecipient>(), verificationUrl);
        if (res.IsError())
            return res;

        // Upsert request
        request.LastEmailsSendAt = DateTime.UtcNow;
        documentSession.Store(request);
        await documentSession.SaveChangesAsync();

        // Log the email verification request
        logger.LogInformation("User {Id} email verification request sent", request.UserId);

        // Publish event
        var verificationEmailSent = request.Adapt<EmailVerificationRequestSent>();
        await bus.PublishAsync(verificationEmailSent);
        return Result.Ok(verificationEmailSent);
    }
}