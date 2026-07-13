using Platform.Core.Events;
using Platform.Core.Lifecycle;

namespace Platform.Core;

/// <summary>
/// Base class every Business Object in every module derives from. Gives every module, for free:
/// identity, the shared lifecycle (docs/architecture/02-business-object-model.md §1.1), extension
/// fields, relations, audit columns, and automatic domain-event emission on every status transition.
///
/// A module's own BO (e.g. Modules.Procurement's PurchaseOrder) adds its own header fields and lines,
/// and its own guard delegates for business rules — it never re-implements status handling.
/// </summary>
public abstract class BusinessObject : IBusinessObject
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private readonly List<BusinessObjectReference> _relations = new();

    public Guid Id { get; }
    public string? DocumentNumber { get; private set; }
    public BusinessObjectStatus Status { get; private set; }
    public long RowVersion { get; private set; }

    public string CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }
    public string? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }

    public ExtensionFieldBag ExtensionFields { get; private set; }

    public IReadOnlyCollection<BusinessObjectReference> Relations => _relations.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>The type name used on emitted events and in Relations — override if the CLR type name
    /// shouldn't be the public identity (e.g. versioned BO types). Defaults to the concrete class name.</summary>
    protected virtual string BusinessObjectTypeName => GetType().Name;

    /// <summary>True only in Draft — the one status where hard delete is allowed (doc 02 §1.1).</summary>
    public bool CanHardDelete => !LifecycleEngine.IsPastDraft(Status);

    protected BusinessObject(string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        Id = Guid.NewGuid();
        Status = BusinessObjectStatus.Draft;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        ExtensionFields = ExtensionFieldBag.Empty();
    }

    /// <summary>
    /// Reserved for ORM materialization (e.g. Entity Framework Core rehydrating an existing row).
    /// Application code must never call this — use the (createdBy) constructor to create a new Business
    /// Object. An ORM constructs an empty instance this way and then sets every property (including
    /// get-only ones, via their backing field) by reflection, bypassing the (createdBy) constructor's
    /// "assign a fresh Id, start in Draft" logic entirely, which would otherwise corrupt a record loaded
    /// back from storage.
    /// </summary>
    protected BusinessObject()
    {
        CreatedBy = null!;
        ExtensionFields = null!;
    }

    /// <summary>
    /// Assigns the document number once (from Platform.Core.NumberRanges.INumberRangeService). Whether
    /// this happens at creation or at Submit is a configuration decision (doc 04 §3), not a kernel one —
    /// this method just enforces "assigned exactly once."
    /// </summary>
    public void AssignNumber(string documentNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);
        if (DocumentNumber is not null)
        {
            throw new InvalidOperationException($"Business object {Id} already has document number '{DocumentNumber}'.");
        }

        DocumentNumber = documentNumber;
    }

    public void AddRelation(BusinessObjectReference reference) => _relations.Add(reference);

    /// <summary>
    /// Attempts the given transition. <paramref name="guard"/> is the caller's business-rule check
    /// (e.g. "requires second approval above 100,000 SAR") — the kernel only enforces that the
    /// transition is structurally legal from the current status; it has no opinion on business rules.
    /// Throws <see cref="InvalidLifecycleTransitionException"/> if structurally illegal, or
    /// <see cref="InvalidOperationException"/> if the guard rejects it.
    /// </summary>
    public void Transition(BusinessObjectTransition transition, string actor, Func<bool>? guard = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        if (guard is not null && !guard())
        {
            throw new InvalidOperationException(
                $"Business rule guard rejected transition '{transition}' on {BusinessObjectTypeName} {Id}.");
        }

        var from = Status;
        var to = LifecycleEngine.Apply(from, transition);

        Status = to;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
        RowVersion++;

        _domainEvents.Add(BusinessObjectStatusChangedEvent.Create(Id, BusinessObjectTypeName, from, to, transition, actor));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
