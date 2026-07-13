using Platform.Core;

namespace Platform.Audit;

/// <summary>
/// One permanent, tamper-evident record of something that happened to an audited entity
/// (docs/architecture/03-platform-services.md #5). Captures the full "who / what / when / from where / why"
/// set the architecture requires, and is hash-chained to the entry before it so a retroactive edit is
/// computationally evident (see <see cref="Hashing.AuditHasher"/> and <see cref="IAuditLog.VerifyChain"/>).
///
/// Append-only by contract: <see cref="IAuditLog.Append"/> is the only mutation, and in a real deployment
/// the audit schema has no UPDATE/DELETE grants at the DB role level (§5). <see cref="Hash"/> and
/// <see cref="PreviousHash"/> are set by the log at append time — callers never supply them.
/// </summary>
public sealed record AuditEntry(
    Guid Id,
    AuditAction Action,
    DateTimeOffset OccurredAt,
    string ActorPrincipalKey,
    BusinessObjectReference BusinessObject,
    string Summary,
    IReadOnlyList<FieldValueChange> FieldValueChanges,
    string? Source,
    Guid? CorrelationId,
    string? PreviousHash,
    string? Hash)
{
    /// <summary>The literal hash of the first entry in a chain, since it has no predecessor.</summary>
    public const string GenesisPreviousHash = null!;
}
