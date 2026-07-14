using Microsoft.AspNetCore.Mvc;
using Modules.MasterData.Application;
using Platform.Api;

namespace Modules.MasterData.Api;

/// <summary>
/// The first business-module controller — inherits <see cref="PlatformApiController"/> for the shared
/// conventions (paging, error envelope), with its own explicit kebab-case route
/// (docs/architecture/05-engineering-standards.md #2: "API route: kebab-case, plural resource") since the
/// base's default `[controller]` token can't produce a hyphen for a compound resource name.
///
/// Two distinct hardcoded actor literals are used below — <c>MaintainerActor</c> for
/// create/edit/submit endpoints and <c>ApproverActor</c> for approve/reject — rather than one "system/ui"
/// for everything. This isn't cosmetic: it's what makes the Segregation of Duties split registered in
/// <see cref="Modules.MasterData.Application.BusinessPartnerSecurity"/> (a real fraud/compliance control,
/// per <see cref="Modules.MasterData.Domain.BusinessPartner"/>'s own doc comment) actually mean something
/// even without real per-user login — the same maintainer can never also be the approver. Real SSO
/// replacing both literals with the authenticated principal's own id is still deferred — see
/// `Modules.MasterData/README.md`.
/// </summary>
[Route("api/v1/masterdata/business-partners")]
public sealed class BusinessPartnersController : PlatformApiController
{
    private const string MaintainerActor = "system/ui";
    private const string ApproverActor = "system/approver";

    private readonly BusinessPartnerService _service;

    public BusinessPartnersController(BusinessPartnerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try
        {
            query = ODataQuery.Parse(Request.Query);
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<BusinessPartnerDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var partner = await _service.GetAsync(id, cancellationToken);
        return partner is null ? NotFound() : Ok(partner);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBusinessPartnerRequest request, CancellationToken cancellationToken)
    {
        // The company should come from the selected company context once real multi-company selection
        // exists — see Modules.MasterData/README.md for what's deferred.
        const string companyId = "C001";

        try
        {
            var created = await _service.CreateAsync(request, MaintainerActor, companyId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> AddAddress(Guid id, [FromBody] AddBusinessPartnerAddressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AddAddressAsync(id, request, MaintainerActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<IActionResult> AddContact(Guid id, [FromBody] AddBusinessPartnerContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AddContactAsync(id, request, MaintainerActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.SubmitAsync(id, MaintainerActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id, ApproverActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, ApproverActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }
}
