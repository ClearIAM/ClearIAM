namespace CleanIAM.Events.Core.Events.Users.Mfa;

/// <summary>
/// Represents the event of user enabling multifactor authentication (MFA).
/// </summary>
/// <param name="Id">Id of user</param>
public record MfaEnabledForUser(Guid Id);