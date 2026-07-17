using Platform.Core;

namespace Platform.Core.Tests;

/// <summary>
/// A trivial, business-meaningless Business Object used only to prove the kernel works end-to-end,
/// per the Phase 0 exit criteria in ROADMAP.md: "a trivial demo BO can be
/// scaffolded end-to-end — created, submitted, approved via a configured workflow, posted, audited,
/// printed bilingually — proving the whole kernel works before any real business logic is written."
/// (Printing/bilingual rendering depends on Platform.Reporting/Localization, not yet built — this proves
/// everything Platform.Core itself is responsible for.)
/// </summary>
public sealed class DemoBusinessObject : BusinessObject
{
    public decimal Amount { get; }

    public DemoBusinessObject(string createdBy, decimal amount) : base(createdBy)
    {
        Amount = amount;
    }
}
