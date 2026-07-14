using Modules.MasterData.Domain;
using Platform.Core;

namespace Modules.MasterData.Tests;

public class BusinessPartnerTests
{
    [Fact]
    public void A_new_partner_starts_in_draft_with_no_document_number()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Gulf Falcon Trading Co", BusinessRoleType.Supplier);

        Assert.Equal(BusinessObjectStatus.Draft, partner.Status);
        Assert.Null(partner.DocumentNumber);
        Assert.Equal("Gulf Falcon Trading Co", partner.Name);
        var role = Assert.Single(partner.BusinessRoles);
        Assert.Equal(BusinessRoleType.Supplier, role.RoleType);
        Assert.Null(role.Trade);
    }

    [Fact]
    public void A_partner_can_hold_multiple_roles()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Gulf Falcon Trading Co", BusinessRoleType.Supplier);
        partner.AddBusinessRole(BusinessRoleType.Subcontractor, "Electrical");

        Assert.Equal(2, partner.BusinessRoles.Count);
    }

    [Fact]
    public void The_same_role_type_can_be_held_twice_with_different_trades()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Multi-Trade Contracting Co", BusinessRoleType.Subcontractor, "Electrical");
        partner.AddBusinessRole(BusinessRoleType.Subcontractor, "Concrete");

        Assert.Equal(2, partner.BusinessRoles.Count);
    }

    [Fact]
    public void The_same_role_type_and_trade_cannot_be_added_twice()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Test Co", BusinessRoleType.Subcontractor, "Electrical");
        Assert.Throws<ArgumentException>(() => partner.AddBusinessRole(BusinessRoleType.Subcontractor, "Electrical"));
    }

    [Fact]
    public void Government_authority_cannot_be_combined_with_another_role()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Municipality Office", BusinessRoleType.GovernmentAuthority);
        Assert.Throws<InvalidOperationException>(() => partner.AddBusinessRole(BusinessRoleType.Supplier));
    }

    [Fact]
    public void A_role_cannot_be_added_alongside_an_existing_government_authority_role_or_vice_versa()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Test Co", BusinessRoleType.Supplier);
        Assert.Throws<InvalidOperationException>(() => partner.AddBusinessRole(BusinessRoleType.GovernmentAuthority));
    }

    [Fact]
    public void A_role_can_be_removed()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Test Co", BusinessRoleType.Supplier);
        var role = partner.AddBusinessRole(BusinessRoleType.Subcontractor, "Electrical");

        partner.RemoveBusinessRole(role.Id);

        Assert.Single(partner.BusinessRoles);
    }

    [Fact]
    public void Submit_then_approve_reaches_the_active_state_the_same_way_every_BO_does()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Al-Rajhi Construction Co", BusinessRoleType.Client);
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
        var partner = new BusinessPartner("ahmer.bilal", "Suspicious Trading LLC", BusinessRoleType.Supplier);
        partner.Submit("ahmer.bilal");

        partner.Reject("compliance.officer");

        Assert.Equal(BusinessObjectStatus.Rejected, partner.Status);
    }

    [Fact]
    public void Addresses_can_be_added_regardless_of_lifecycle_status()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Approved Vendor Co", BusinessRoleType.Supplier);
        partner.Submit("ahmer.bilal");
        partner.Approve("finance.manager");

        var address = partner.AddAddress(AddressType.SiteOffice, "Saudi Arabia", "Riyadh", "King Fahd Road");

        Assert.Single(partner.Addresses);
        Assert.Equal(AddressType.SiteOffice, address.AddressType);
        Assert.Equal("Riyadh", address.City);
        Assert.Equal(BusinessObjectStatus.Approved, partner.Status); // adding an address never changes lifecycle status
    }

    [Fact]
    public void A_partner_can_have_multiple_addresses_of_the_same_type()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Multi-Site Contracting Co", BusinessRoleType.Supplier);

        partner.AddAddress(AddressType.SiteOffice, "Saudi Arabia", "Riyadh", "Project A Site");
        partner.AddAddress(AddressType.SiteOffice, "Saudi Arabia", "Jeddah", "Project B Site");

        Assert.Equal(2, partner.Addresses.Count);
    }

    [Fact]
    public void An_address_can_be_removed()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Test Vendor", BusinessRoleType.Supplier);
        var address = partner.AddAddress(AddressType.Billing, "Saudi Arabia", "Riyadh", "Olaya Street");

        partner.RemoveAddress(address.Id);

        Assert.Empty(partner.Addresses);
    }

    [Fact]
    public void Contacts_can_be_added_with_their_own_name_and_details()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Approved Vendor Co", BusinessRoleType.Supplier);

        var contact = partner.AddContact("Fahad Al-Otaibi", "Procurement Manager", "fahad@vendor.example", "+966500000000");

        Assert.Single(partner.Contacts);
        Assert.Equal("Fahad Al-Otaibi", contact.Name);
        Assert.Equal("Procurement Manager", contact.JobTitle);
    }

    [Fact]
    public void A_contact_can_be_removed()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Test Vendor", BusinessRoleType.Supplier);
        var contact = partner.AddContact("Sara Al-Harbi", "Accountant", "sara@vendor.example", "+966511111111");

        partner.RemoveContact(contact.Id);

        Assert.Empty(partner.Contacts);
    }

    [Fact]
    public void Tax_registration_number_can_be_set_independently()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Test Vendor", BusinessRoleType.Supplier);

        partner.UpdateTaxRegistrationNumber("300000000000003");

        Assert.Equal("300000000000003", partner.TaxRegistrationNumber);
    }

    [Fact]
    public void Arabic_name_is_null_until_set_and_can_be_set_independently()
    {
        var partner = new BusinessPartner("ahmer.bilal", "Gulf Falcon Trading Co", BusinessRoleType.Supplier);
        Assert.Null(partner.NameArabic);

        partner.UpdateNameArabic("شركة صقر الخليج التجارية");

        Assert.Equal("شركة صقر الخليج التجارية", partner.NameArabic);
    }
}
