namespace Modules.MasterData.Domain;

/// <summary>
/// One admin-configurable picklist category — e.g. "Country", "BusinessRoleType", "UnitOfMeasure" — per
/// CLAUDE.md's explicit instruction not to hard-code lookup/classification data (business words like
/// "Customer"/"Vendor" must be admin-editable, the same way SAP's domain/value-table maintenance (SM30) or
/// Dynamics 365's Option Sets let an administrator add a new picklist value without a code change).
///
/// Deliberately not a <see cref="BusinessObject"/>: a picklist category has no Draft/Submit/Approve
/// lifecycle in any real ERP — SAP's table maintenance and D365's Option Set editor both apply immediately,
/// gated by an authorization role, not an approval workflow. Consistent with how
/// <c>Platform.Configuration</c>'s own values are immediate-effect, not workflowed.
/// </summary>
public sealed class LookupType
{
    public Guid Id { get; private set; }

    /// <summary>The stable key other code references (e.g. <c>"Country"</c>) — never shown to the user,
    /// never renamed once created (values reference it by this code, same immutable-key reasoning as
    /// <see cref="GLAccount.AccountCode"/>).</summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? NameArabic { get; private set; }

    /// <summary>True for the lookup types this platform's own code seeds and depends on by
    /// <see cref="LookupValue.Code"/> (Country/BusinessRoleType/AddressType/UnitOfMeasure/Trade) — the
    /// *category* can never be deleted (deleting it would break every field that references it), but its
    /// *values* remain fully admin add/edit/deactivate/delete like any other lookup type's values. A
    /// non-system-defined lookup type (one an administrator created from scratch, e.g. a future "Incoterms"
    /// list) can be deleted outright once it has no values left.</summary>
    public bool IsSystemDefined { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public LookupType(string createdBy, string code, string name, string? nameArabic, bool isSystemDefined)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        NameArabic = nameArabic;
        IsSystemDefined = isSystemDefined;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private LookupType()
    {
        Code = null!;
        Name = null!;
        CreatedBy = null!;
    }

    public void Rename(string actor, string name, string? nameArabic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        NameArabic = nameArabic;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
