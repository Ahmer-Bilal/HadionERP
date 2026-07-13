using Modules.MasterData.Domain;
using Platform.Core.NumberRanges;

namespace Modules.MasterData.Application;

/// <summary>
/// Orchestrates Business Partner use cases — validates input, drives the Domain object, and persists
/// through the repository port. No business rules live here (those belong on
/// <see cref="BusinessPartner"/> itself, per docs/architecture/01-architecture-foundation.md #1); this
/// layer only coordinates.
/// </summary>
public sealed class BusinessPartnerService
{
    /// <summary>The number range key this module registers for Business Partners — matches the naming
    /// convention "{ModuleAbbrev}-{DocAbbrev}" in docs/architecture/05-engineering-standards.md #2.</summary>
    public const string NumberRangeKey = "MD-BP";

    private readonly IBusinessPartnerRepository _repository;
    private readonly INumberRangeService _numberRangeService;

    public BusinessPartnerService(IBusinessPartnerRepository repository, INumberRangeService numberRangeService)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
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
        partner.UpdateContactDetails(request.Email, request.Phone, request.Country, request.City, request.AddressLine);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        partner.AssignNumber(documentNumber);

        _repository.Add(partner);
        await _repository.SaveChangesAsync(cancellationToken);

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

    public async Task<BusinessPartnerDto> UpdateContactAsync(
        Guid id, UpdateBusinessPartnerContactRequest request, CancellationToken cancellationToken = default)
    {
        var partner = await RequirePartnerAsync(id, cancellationToken);
        partner.UpdateContactDetails(request.Email, request.Phone, request.Country, request.City, request.AddressLine);
        await _repository.SaveChangesAsync(cancellationToken);
        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        var partner = await RequirePartnerAsync(id, cancellationToken);
        partner.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        var partner = await RequirePartnerAsync(id, cancellationToken);
        partner.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        return ToDto(partner);
    }

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
        partner.Email,
        partner.Phone,
        partner.Country,
        partner.City,
        partner.AddressLine,
        partner.CreatedAt,
        partner.CreatedBy);
}
