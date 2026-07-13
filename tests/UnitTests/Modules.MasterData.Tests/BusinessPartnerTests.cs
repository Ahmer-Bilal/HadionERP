using Modules.MasterData.Domain;
using Platform.Core;

namespace Modules.MasterData.Tests;

public class BusinessPartnerTests
{
    [Fact]
    public void A_new_partner_starts_in_draft_with_no_document_number()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Gulf Falcon Trading Co", PartnerType.Vendor);

        Assert.Equal(BusinessObjectStatus.Draft, partner.Status);
        Assert.Null(partner.DocumentNumber);
        Assert.Equal("Gulf Falcon Trading Co", partner.Name);
        Assert.Equal(PartnerType.Vendor, partner.PartnerType);
    }

    [Fact]
    public void Submit_then_approve_reaches_the_active_state_the_same_way_every_BO_does()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Al-Rajhi Construction Co", PartnerType.Customer);
        partner.AssignNumber("MD-BP-2026-000001");

        partner.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, partner.Status);

        partner.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, partner.Status);
        Assert.Equal(2, partner.DomainEvents.Count); // one event each for Submit and Approve
    }

    [Fact]
    public void Reject_returns_a_submitted_partner_to_the_rejected_state()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Suspicious Trading LLC", PartnerType.Vendor);
        partner.Submit("ahmer.bilal");

        partner.Reject("compliance.officer");

        Assert.Equal(BusinessObjectStatus.Rejected, partner.Status);
    }

    [Fact]
    public void Contact_details_can_be_updated_regardless_of_lifecycle_status()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Approved Vendor Co", PartnerType.Vendor);
        partner.Submit("ahmer.bilal");
        partner.Approve("finance.manager");

        partner.UpdateContactDetails("new@vendor.example", "+966500000000", "Saudi Arabia", "Riyadh", "King Fahd Road");

        Assert.Equal("new@vendor.example", partner.Email);
        Assert.Equal("Riyadh", partner.City);
        Assert.Equal(BusinessObjectStatus.Approved, partner.Status); // updating contact info never changes lifecycle status
    }

    [Fact]
    public void Tax_registration_number_can_be_set_independently()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Test Vendor", PartnerType.Vendor);

        partner.UpdateTaxRegistrationNumber("300000000000003");

        Assert.Equal("300000000000003", partner.TaxRegistrationNumber);
    }
}
