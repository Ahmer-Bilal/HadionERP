using Platform.Core;

namespace Modules.Finance.Domain;

/// <summary>
/// One General Ledger journal entry — the Universal-Journal-style line-item document every financial fact
/// in this platform eventually reduces to
/// (docs/architecture/07-integrated-project-controlling.md #1), and the first Finance
/// Business Object, matching the Phase 1 exit criteria's "post/reverse a GL journal ... with full audit
/// trail" (ROADMAP.md). This is the first real use anywhere in this codebase of the
/// full Draft → Submit → Approve → Post → Reverse lifecycle
/// (Platform.Core.Lifecycle.LifecycleEngine already supported Post/Reverse from Phase 0, but no Business
/// Object had exercised that path until now — every Master Data slice stops at Approved).
///
/// The one real business rule Domain enforces here — <see cref="IsBalanced"/> — is not a configurable
/// threshold (CLAUDE.md's "don't hard-code business rules... that should be configuration" doesn't apply
/// to this): total debits equal total credits is the accounting identity double-entry bookkeeping is
/// built on, true for every company, every era, no configuration point makes sense for it.
///
/// Reversal is handled the same "correct by reversal, not by deletion" way as everywhere else in this
/// platform, but for a Posted financial document that means something specific: reversing does not just
/// flip this entry's own status to Reversed (though it does — see <see cref="Reverse"/>), it also requires
/// a brand-new mirror entry with every line's debit/credit swapped, so the ledger's balance is actually
/// undone. Building that mirror entry needs a document number and audit trail of its own, so it's built by
/// <c>JournalEntryService.ReverseAsync</c> (Application layer), not by this entity reversing itself —
/// <see cref="MarkAsReversalOf"/> is the one piece of state this entity needs to carry that link.
/// </summary>
public sealed class JournalEntry : BusinessObject
{
    private readonly List<JournalLine> _lines = new();

    public DateOnly PostingDate { get; private set; }

    public string Description { get; private set; }

    /// <summary>Set once, by <c>JournalEntryService.ReverseAsync</c>, on the brand-new mirror entry it
    /// creates when reversing a Posted entry — null for every normal entry. Lets the reversal be traced
    /// back to what it reverses without guessing from description text.</summary>
    public Guid? ReversalOfEntryId { get; private set; }

    /// <summary>What raised this entry — one of the constants in <c>JournalEntrySourceDocumentTypes</c>
    /// (Application layer; Domain only stores the string, it doesn't need to know the closed set). Null only
    /// for entries created before this field existed; every entry created through
    /// <c>JournalEntryService.CreateAsync</c> (a human, tagged "Manual") or
    /// <c>CreateSystemGeneratedAsync</c> (another Finance document's own posting) now sets it. Answers the
    /// gap analysis's "what created this entry" question (Journal Entry detail's Document Flow panel,
    /// Journal List's Source column) without the UI having to guess from description text or reach across
    /// modules to ask every document type "did you create this."</summary>
    public string? SourceDocumentType { get; private set; }

    /// <summary>The specific document's own Id when <see cref="SourceDocumentType"/> names a real document
    /// type (an AP/AR Invoice, a Payment, a Customer Receipt) — null for "Manual" (there is no other
    /// document) and for entries predating this field.</summary>
    public Guid? SourceDocumentId { get; private set; }

    /// <summary>The lines making up this entry — 0..n child collection, only exists through this parent,
    /// same pattern as Modules.MasterData's <c>BusinessPartner.Addresses</c>.</summary>
    public IReadOnlyCollection<JournalLine> Lines => _lines.AsReadOnly();

    public decimal TotalDebits => _lines.Sum(l => l.DebitAmount);

    public decimal TotalCredits => _lines.Sum(l => l.CreditAmount);

    /// <summary>True only once there is at least one line and the accounting identity holds. An entry
    /// with zero lines is never "balanced" — it's incomplete, not balanced by vacuous truth.</summary>
    public bool IsBalanced => _lines.Count > 0 && TotalDebits == TotalCredits;

    public JournalEntry(string createdBy, DateOnly postingDate, string description)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        PostingDate = postingDate;
        Description = description;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private JournalEntry()
    {
        Description = null!;
    }

    /// <summary>Adds one line. Exactly one of <paramref name="debitAmount"/>/<paramref name="creditAmount"/>
    /// must be positive and the other exactly zero — a line is either a debit or a credit, never both,
    /// never neither. Amounts can be added/removed freely while still in Draft; once Submitted, lines are
    /// frozen the same way Business Partner's Addresses/Contacts are NOT frozen (a deliberate difference:
    /// unlike an address correction, changing a journal line after submission would silently invalidate
    /// whatever approval was already given against the original amounts).</summary>
    public JournalLine AddLine(Guid glAccountId, Guid? costCenterId, decimal debitAmount, decimal creditAmount, string? lineDescription = null)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the journal entry is in Draft.");
        if (debitAmount < 0 || creditAmount < 0)
            throw new ArgumentException("Debit and credit amounts cannot be negative.");
        if ((debitAmount > 0) == (creditAmount > 0))
            throw new ArgumentException("Exactly one of debitAmount/creditAmount must be positive, the other zero.");

        var line = new JournalLine(glAccountId, costCenterId, debitAmount, creditAmount, lineDescription);
        _lines.Add(line);
        return line;
    }

    public void MarkAsReversalOf(Guid originalEntryId) => ReversalOfEntryId = originalEntryId;

    /// <summary>Records what raised this entry. Callable only while Draft — same "set once, before the
    /// entry starts its real lifecycle" timing as <see cref="MarkAsReversalOf"/> — since a document's
    /// origin is a fact about its creation, not something that should ever change afterward.</summary>
    public void MarkSourceDocument(string sourceDocumentType, Guid? sourceDocumentId)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("The source document can only be set while the journal entry is in Draft.");
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDocumentType);
        SourceDocumentType = sourceDocumentType;
        SourceDocumentId = sourceDocumentId;
    }

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);

    /// <summary>Posts the entry — the point at which it has a real financial effect. Refuses to post an
    /// unbalanced entry; this is a structural accounting guarantee, checked here rather than trusted to
    /// whoever approved it.</summary>
    public void Post(string actor)
    {
        if (!IsBalanced)
            throw new InvalidOperationException(
                $"Journal entry does not balance (debits {TotalDebits}, credits {TotalCredits}) and cannot be posted.");

        Transition(BusinessObjectTransition.Post, actor);
    }

    public void Reverse(string actor) => Transition(BusinessObjectTransition.Reverse, actor);
}
