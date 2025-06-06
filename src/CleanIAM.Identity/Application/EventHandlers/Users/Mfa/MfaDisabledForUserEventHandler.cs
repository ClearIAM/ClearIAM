using CleanIAM.Events.Core.Events.Users.Mfa;
using CleanIAM.Identity.Core.Users;
using Marten;

namespace CleanIAM.Identity.Application.EventHandlers.Users.Mfa;

public class MfaDisabledForUserEventHandler
{
    public static async Task HandleAsync(MfaDisabledForUser mfaDisabled, IDocumentSession session,
        CancellationToken cancellationToken, ILogger logger)
    {
        var user = await session.LoadAsync<IdentityUser>(mfaDisabled.Id, cancellationToken);
        if (user is null)
        {
            logger.LogError($"[MfaDisabledForUserEventHandler] Could not find user {mfaDisabled.Id}");
            return;
        }

        // Update the user in database
        user.IsMFAEnabled = false;
        session.Update(user);
        await session.SaveChangesAsync(cancellationToken);
    }
}