namespace Platform.Api;

/// <summary>
/// The unified error response shape (RFC 7807 problem-details-inspired) every error response uses
/// (docs/architecture/04-data-and-api.md #2). A frontend or external integrator handles errors uniformly
/// rather than each endpoint inventing its own error JSON.
///
/// <see cref="Errors"/> carries field-level validation failures (e.g. "Amount must be positive") — the
/// same structure a frontend form uses to highlight specific fields, not just show a generic message.
/// </summary>
public sealed record ApiErrorEnvelope(
    string Type,
    string Title,
    int Status,
    string? Detail,
    IReadOnlyDictionary<string, string[]> Errors)
{
    /// <summary>Builds a 400 validation error with field-level failures.</summary>
    public static ApiErrorEnvelope Validation(IReadOnlyDictionary<string, string[]> errors, string? detail = null) =>
        new("https://httpstatuses.io/400", "Validation failed", 400, detail, errors);

    /// <summary>Builds a 409 conflict error (e.g. optimistic concurrency mismatch, duplicate doc number).</summary>
    public static ApiErrorEnvelope Conflict(string detail) =>
        new("https://httpstatuses.io/409", "Conflict", 409, detail, new Dictionary<string, string[]>());

    /// <summary>Builds a 400 bad-request error with a single message (no field-level detail).</summary>
    public static ApiErrorEnvelope BadRequest(string detail) =>
        new("https://httpstatuses.io/400", "Bad request", 400, detail, new Dictionary<string, string[]>());
}
