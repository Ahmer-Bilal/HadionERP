namespace Modules.MasterData.Domain;

/// <summary>The purpose of one of a Business Partner's (possibly several) addresses. A partner can have
/// more than one address of the same type — e.g. a contractor with several active Site Office addresses,
/// one per project.</summary>
public enum AddressType
{
    HeadOffice,
    Billing,
    Shipping,
    SiteOffice
}
