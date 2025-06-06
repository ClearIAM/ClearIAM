namespace CleanIAM.Events.Core.Events.Scopes;

/// <summary>
/// Event that is published when a scope is deleted.
/// </summary>
/// <param name="Name">name of the deleted scope</param>
public record ScopeDeleted(string Name);