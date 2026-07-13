namespace Modules.MasterData.Domain;

/// <summary>Whether a Business Partner can be used as a customer (AR), a vendor (AP), or both — the
/// same physical record either way, matching docs/architecture/01-architecture-foundation.md #3.1
/// ("Business Partners... referenced by every transactional module").</summary>
public enum PartnerType
{
    Customer,
    Vendor,
    Both
}
