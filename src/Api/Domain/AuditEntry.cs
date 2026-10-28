namespace Api.Domain;

/// <summary>
/// Who did what, and when. Holds identifiers only, never SSNs or bureau payloads.
/// </summary>
public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public AuditEntry(string actor, string action, string? target, DateTimeOffset occurredAt)
    {
        Actor = actor;
        Action = action;
        Target = target;
        OccurredAt = occurredAt;
    }

    public int Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string Actor { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string? Target { get; private set; }
}
