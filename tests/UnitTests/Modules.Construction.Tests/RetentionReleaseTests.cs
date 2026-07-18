using Modules.Construction.Domain;
using Platform.Core;

namespace Modules.Construction.Tests;

public class RetentionReleaseTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CommercialDocumentId = Guid.NewGuid();
    private static readonly DateOnly ReleaseDate = new(2026, 8, 1);

    private static RetentionRelease NewRelease(decimal amountReleased = 200m) =>
        new("ahmer.bilal", ProjectId, CommercialDocumentType.Contract, CommercialDocumentId, ReleaseDate, amountReleased, RetentionTriggerEvent.Manual);

    [Fact]
    public void A_new_release_starts_in_draft_with_no_document_number()
    {
        var release = NewRelease();

        Assert.Equal(BusinessObjectStatus.Draft, release.Status);
        Assert.Null(release.DocumentNumber);
        Assert.Null(release.LinkedArInvoiceId);
        Assert.Null(release.LinkedApInvoiceId);
    }

    [Fact]
    public void Zero_or_negative_amount_released_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => NewRelease(amountReleased: 0m));
        Assert.Throws<ArgumentException>(() => NewRelease(amountReleased: -1m));
    }

    [Fact]
    public void Billing_accounts_are_null_by_default_and_settable_via_the_constructor()
    {
        var withoutAccounts = NewRelease();
        Assert.Null(withoutAccounts.RevenueAccountId);
        Assert.Null(withoutAccounts.ReceivableAccountId);

        var revenueAccountId = Guid.NewGuid();
        var receivableAccountId = Guid.NewGuid();
        var withAccounts = new RetentionRelease(
            "ahmer.bilal", ProjectId, CommercialDocumentType.Contract, CommercialDocumentId, ReleaseDate, 200m,
            RetentionTriggerEvent.TakingOver, revenueAccountId, receivableAccountId);

        Assert.Equal(revenueAccountId, withAccounts.RevenueAccountId);
        Assert.Equal(receivableAccountId, withAccounts.ReceivableAccountId);
        Assert.Equal(RetentionTriggerEvent.TakingOver, withAccounts.TriggerEvent);
    }

    [Fact]
    public void LinkArInvoice_sets_the_link()
    {
        var release = NewRelease();
        var arInvoiceId = Guid.NewGuid();
        release.LinkArInvoice(arInvoiceId);
        Assert.Equal(arInvoiceId, release.LinkedArInvoiceId);
    }

    [Fact]
    public void LinkApInvoice_sets_the_link()
    {
        var release = NewRelease();
        var apInvoiceId = Guid.NewGuid();
        release.LinkApInvoice(apInvoiceId);
        Assert.Equal(apInvoiceId, release.LinkedApInvoiceId);
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var release = NewRelease();
        release.AssignNumber("CON-RETREL-2026-000001");

        release.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, release.Status);

        release.Approve("commercial.manager");
        Assert.Equal(BusinessObjectStatus.Approved, release.Status);
    }

    [Fact]
    public void Reject_transitions_to_rejected()
    {
        var release = NewRelease();
        release.AssignNumber("CON-RETREL-2026-000001");
        release.Submit("ahmer.bilal");

        release.Reject("commercial.manager");
        Assert.Equal(BusinessObjectStatus.Rejected, release.Status);
    }
}
