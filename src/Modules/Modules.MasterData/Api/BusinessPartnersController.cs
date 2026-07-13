using Microsoft.AspNetCore.Mvc;
using Modules.MasterData.Application;
using Platform.Api;

namespace Modules.MasterData.Api;

/// <summary>
/// The first business-module controller — inherits <see cref="PlatformApiController"/> for the shared
/// conventions (paging, error envelope), with its own explicit kebab-case route
/// (docs/architecture/05-engineering-standards.md #2: "API route: kebab-case, plural resource") since the
/// base's default `[controller]` token can't produce a hyphen for a compound resource name.
/// </summary>
[Route("api/v1/masterdata/business-partners")]
public sealed class BusinessPartnersController : PlatformApiController
{
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
        // The actor/company should come from the authenticated principal + selected company context once
        // real SSO and multi-company selection exist — see Modules.MasterData/README.md for what's deferred.
        const string actor = "system/ui";
        const string companyId = "C001";

        try
        {
            var created = await _service.CreateAsync(request, actor, companyId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
    }

    [HttpPut("{id:guid}/contact")]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] UpdateBusinessPartnerContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.UpdateContactAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        const string actor = "system/ui";

        try
        {
            return Ok(await _service.SubmitAsync(id, actor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        const string actor = "system/ui";

        try
        {
            return Ok(await _service.ApproveAsync(id, actor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
    }
}
