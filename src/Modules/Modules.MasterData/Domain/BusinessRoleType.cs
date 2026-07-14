namespace Modules.MasterData.Domain;

/// <summary>
/// The roles a Business Partner can hold — replaces the old single <c>PartnerType</c> (Customer/Vendor/
/// Both) enum per docs/architecture/06-roadmap.md's Phase 2 design (captured 2026-07-14, implemented once
/// Phase 2 was actually reached, per that design's own stated intent — "document now, build later"). A
/// real construction-industry partner commonly holds several roles at once (a company can be both a
/// Supplier and a Subcontractor), which a single enum could never express.
///
/// "Client" is the construction-industry label for what SAP calls Customer/Debtor — modeled as one role,
/// not two, per the roadmap's explicit instruction not to duplicate the same AR-invoiced counterparty
/// concept under two names.
/// </summary>
public enum BusinessRoleType
{
    Client,
    Supplier,
    Subcontractor,
    Consultant,
    JointVenturePartner,
    GovernmentAuthority,
    RentalCompany,
    Manufacturer,
    ManpowerSupplier,
    TestingLaboratory,
}
