using System.Text.Json;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;

namespace Modules.MasterData.Application;

/// <summary>
/// Orchestrates Business Partner use cases — validates input, drives the Domain object, and persists
/// through the repository port. No business rules live here (those belong on
/// <see cref="BusinessPartner"/> itself, per docs/architecture/01-architecture-foundation.md #1); this
/// layer only coordinates. Audit is likewise a platform service, not module logic (CLAUDE.md /
/// docs/architecture/03-platform-services.md #5) — this layer only calls <see cref="IAuditRecorder"/> at
/// the points a real auditor would care about (created, address/contact added, status transitions), the
/// actual capture/hash-chaining lives entirely in Platform.Audit.
/// </summary>
public sealed class BusinessPartnerService
{
    /// <summary>The number range key this module registers for Business Partners — matches the naming
    /// convention "{ModuleAbbrev}-{DocAbbrev}" in docs/architecture/05-engineering-standards.md #2.</summary>
    public const string NumberRangeKey = "MD-BP";

    private const string AuditTargetType = "BusinessPartner";
    private const string AuditSource = "Modules.MasterData";

    private readonly IBusinessPartnerRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;

    public BusinessPartnerService(
        IBusinessPartnerRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
    }

    public async Task<BusinessPartnerDto> CreateAsync(
        CreateBusinessPartnerRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PartnerType>(request.PartnerType, ignoreCase: true, out var partnerType))
        {
            throw new ArgumentException($"Invalid partner type '{request.PartnerType}'. Expected Customer, Vendor, or Both.");
        }

        var partner = new BusinessPartner(actor, request.Name, partnerType);
        partner.UpdateTaxRegistrationNumber(request.TaxRegistrationNumber);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        partner.AssignNumber(documentNumber);

        _repository.Add(partner);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(
            AuditReference(partner.Id),
            actor,
            $"Business partner '{partner.Name}' ({partner.DocumentNumber}) created.",
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partner = await _repository.GetAsync(id, cancellationToken);
        return partner is null ? null : ToDto(partner);
    }

    public async Task<(IReadOnlyList<BusinessPartnerDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<BusinessPartnerDto> AddAddressAsync(
        Guid id, AddBusinessPartnerAddressRequest request, string actor, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AddressType>(request.AddressType, ignoreCase: true, out var addressType))
        {
            throw new ArgumentException(
                $"Invalid address type '{request.AddressType}'. Expected HeadOffice, Billing, Shipping, or SiteOffice.");
        }

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var address = partner.AddAddress(addressType, request.Country, request.City, request.AddressLine);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Address added to '{partner.Name}'.",
            new[]
            {
                new FieldValueChange(
                    "Addresses",
                    OldValueJson: null,
                    NewValueJson: JsonSerializer.Serialize(new BusinessPartnerAddressDto(
                        address.Id, address.AddressType.ToString(), address.Country, address.City, address.AddressLine)))
            },
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> AddContactAsync(
        Guid id, AddBusinessPartnerContactRequest request, string actor, CancellationToken cancellationToken = default)
    {
        var partner = await RequirePartnerAsync(id, cancellationToken);
        var contact = partner.AddContact(request.Name, request.JobTitle, request.Email, request.Phone);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Contact added to '{partner.Name}'.",
            new[]
            {
                new FieldValueChange(
                    "Contacts",
                    OldValueJson: null,
                    NewValueJson: JsonSerializer.Serialize(new BusinessPartnerContactDto(
                        contact.Id, contact.Name, contact.JobTitle, contact.Email, contact.Phone)))
            },
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        var partner = await RequirePartnerAsync(id, cancellationToken);
        var fromStatus = partner.Status;
        partner.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(
            AuditReference(partner.Id),
            actor,
            $"Business partner '{partner.Name}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()),
            JsonSerializer.Serialize(partner.Status.ToString()),
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        var partner = await RequirePartnerAsync(id, cancellationToken);
        var fromStatus = partner.Status;
        partner.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(
            AuditReference(partner.Id),
            actor,
            $"Business partner '{partner.Name}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()),
            JsonSerializer.Serialize(partner.Status.ToString()),
            AuditSource);

        return ToDto(partner);
    }

    private static BusinessObjectReference AuditReference(Guid partnerId) => new(partnerId, AuditTargetType, "Self");

    private async Task<BusinessPartner> RequirePartnerAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Business partner {id} was not found.");

    private static BusinessPartnerDto ToDto(BusinessPartner partner) => new(
        partner.Id,
        partner.DocumentNumber,
        partner.Status.ToString(),
        partner.Name,
        partner.PartnerType.ToString(),
        partner.TaxRegistrationNumber,
        partner.Addresses.Select(a => new BusinessPartnerAddressDto(a.Id, a.AddressType.ToString(), a.Country, a.City, a.AddressLine)).ToList(),
        partner.Contacts.Select(c => new BusinessPartnerContactDto(c.Id, c.Name, c.JobTitle, c.Email, c.Phone)).ToList(),
        partner.CreatedAt,
        partner.CreatedBy);
}
