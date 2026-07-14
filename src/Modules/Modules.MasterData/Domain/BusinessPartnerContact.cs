namespace Modules.MasterData.Domain;

/// <summary>
/// One of a Business Partner's (possibly several) contact people — a company has a Procurement Manager,
/// an Accountant, a CEO, a Site Engineer, each with their own phone/email, not one shared number for the
/// whole company. A child entity, same pattern and same "constructed only through the parent" rule as
/// <see cref="BusinessPartnerAddress"/>. Job title is deliberately free text, not a fixed list — titles
/// vary too much between companies to constrain usefully.
/// </summary>
public sealed class BusinessPartnerContact
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? JobTitle { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    internal BusinessPartnerContact(string name, string? jobTitle, string? email, string? phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = Guid.NewGuid();
        Name = name;
        JobTitle = jobTitle;
        Email = email;
        Phone = phone;
    }

    /// <summary>Reserved for ORM materialization. Never call from application code.</summary>
    private BusinessPartnerContact()
    {
        Name = null!;
    }
}
