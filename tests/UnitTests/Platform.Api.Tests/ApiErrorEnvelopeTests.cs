namespace Platform.Api.Tests;

/// <summary>
/// Proves <see cref="ApiErrorEnvelope"/> produces the correct shape for each error kind — the unified
/// response every endpoint returns on failure. Field-level validation errors must carry per-field messages
/// so a frontend form can highlight specific fields, not just show a generic message.
/// </summary>
public class ApiErrorEnvelopeTests
{
    [Fact]
    public void Validation_carries_field_level_errors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Amount"] = new[] { "Must be positive.", "Must not exceed 100000." },
            ["VendorId"] = new[] { "Vendor is required." },
        };

        var envelope = ApiErrorEnvelope.Validation(errors, "Please correct the highlighted fields.");

        Assert.Equal(400, envelope.Status);
        Assert.Equal("Validation failed", envelope.Title);
        Assert.Equal("Please correct the highlighted fields.", envelope.Detail);
        Assert.Equal(2, envelope.Errors.Count);
        Assert.Equal(2, envelope.Errors["Amount"].Length);
        Assert.Single(envelope.Errors["VendorId"]);
    }

    [Fact]
    public void Conflict_has_409_status_and_detail()
    {
        var envelope = ApiErrorEnvelope.Conflict("Document PROC-PO-2026-000123 was modified by another user.");

        Assert.Equal(409, envelope.Status);
        Assert.Equal("Conflict", envelope.Title);
        Assert.Contains("PROC-PO-2026-000123", envelope.Detail!);
        Assert.Empty(envelope.Errors);
    }

    [Fact]
    public void BadRequest_has_400_status_and_no_field_errors()
    {
        var envelope = ApiErrorEnvelope.BadRequest("An Idempotency-Key header is required.");

        Assert.Equal(400, envelope.Status);
        Assert.Equal("Bad request", envelope.Title);
        Assert.Empty(envelope.Errors);
    }

    [Fact]
    public void Forbidden_has_403_status_and_no_field_errors()
    {
        var envelope = ApiErrorEnvelope.Forbidden("Principal 'ahmer.bilal' holds no Duty granting 'MasterData.BusinessPartner.Approve'.");

        Assert.Equal(403, envelope.Status);
        Assert.Equal("Forbidden", envelope.Title);
        Assert.Contains("MasterData.BusinessPartner.Approve", envelope.Detail!);
        Assert.Empty(envelope.Errors);
    }

    [Fact]
    public void Conflict_has_a_distinct_type_uri_from_client_errors()
    {
        // Validation and BadRequest are both 400-class errors, so they share a type URI — that's correct,
        // they're the same HTTP status. Conflict (409) is a different status, so its type URI differs.
        var validation = ApiErrorEnvelope.Validation(new Dictionary<string, string[]>());
        var badRequest = ApiErrorEnvelope.BadRequest("detail");
        var conflict = ApiErrorEnvelope.Conflict("detail");

        Assert.Equal(validation.Type, badRequest.Type); // both 400
        Assert.NotEqual(validation.Type, conflict.Type); // 400 vs 409
    }
}
