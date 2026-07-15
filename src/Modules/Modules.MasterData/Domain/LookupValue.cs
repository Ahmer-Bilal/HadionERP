namespace Modules.MasterData.Domain;

/// <summary>
/// One admin-managed value within a <see cref="LookupType"/> category (e.g. Code <c>"SA"</c>, Name
/// <c>"Saudi Arabia"</c> within the <c>"Country"</c> type). See <see cref="LookupType"/>'s own doc comment
/// for why this has no Draft/Submit/Approve lifecycle — it's immediate-effect reference data, gated by a
/// security role, the same as SAP domain-value maintenance or a D365 Option Set entry.
/// </summary>
public sealed class LookupValue
{
    public Guid Id { get; private set; }

    /// <summary>Which <see cref="LookupType.Code"/> this value belongs to.</summary>
    public string LookupTypeCode { get; private set; }

    /// <summary>The stable key other records reference (e.g. <c>"SA"</c>, <c>"GovernmentAuthority"</c>) —
    /// unique within its <see cref="LookupTypeCode"/>, never reused once referenced by real data.</summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? NameArabic { get; private set; }

    /// <summary>True when the value can be newly selected. Deactivating (rather than deleting) a value
    /// that's already referenced by existing records keeps those records valid while preventing new ones —
    /// the same "correct by reversal, not by deletion" principle used everywhere else in this platform.
    /// Deletion is still offered (see <c>LookupService.DeleteValueAsync</c>) but only when nothing
    /// references the value yet, mirroring how SAP blocks deleting a domain value still in use.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Display order within its type (ascending). Lets an admin put the most-used values first in
    /// a long list (e.g. neighboring GCC countries before the rest of the alphabet) without renaming codes.</summary>
    public int SortOrder { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public LookupValue(string createdBy, string lookupTypeCode, string code, string name, string? nameArabic, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(lookupTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        LookupTypeCode = lookupTypeCode;
        Code = code;
        Name = name;
        NameArabic = nameArabic;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private LookupValue()
    {
        LookupTypeCode = null!;
        Code = null!;
        Name = null!;
        CreatedBy = null!;
    }

    public void Update(string actor, string name, string? nameArabic, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        NameArabic = nameArabic;
        SortOrder = sortOrder;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Activate(string actor)
    {
        IsActive = true;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate(string actor)
    {
        IsActive = false;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}
