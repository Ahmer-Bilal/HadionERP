using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Identity.Application;
using Platform.Api;

namespace Modules.Identity.Api;

/// <summary>
/// The Users admin panel's controller — create/deactivate users and manage their role assignments. One
/// actor-gated privilege (<see cref="IdentitySecurity.AdministerPrivilegeKey"/>), no Maintainer/Approver
/// split, same reasoning as <c>Modules.MasterData.Api.LookupsController</c>. This is the one endpoint in
/// the whole solution where a 409 doesn't mean "optimistic concurrency conflict" — it means "this role
/// assignment has an unresolved Segregation of Duties conflict," see
/// <see cref="Modules.Identity.Application.SodConflictException"/>.
/// </summary>
[Route("api/v1/identity/users")]
public sealed class UsersController : PlatformApiController
{
    private readonly UserService _userService;

    public UsersController(UserService userService) => _userService = userService;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _userService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _userService.CreateAsync(request, CurrentActor, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPut("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _userService.UpdateProfileAsync(id, request, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _userService.ResetPasswordAsync(id, request, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _userService.SetActiveAsync(id, isActive: true, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _userService.SetActiveAsync(id, isActive: false, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _userService.AssignRoleAsync(id, request, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (SodConflictException ex)
        {
            // Structured, not just a message string — the Users admin UI lists each conflict individually
            // and requires the admin to type an override reason before retrying with AssignRoleRequest's
            // OverrideReason set (see SodConflictException's own doc comment for the "never silent" design).
            var errors = ex.Conflicts
                .Select((c, i) => (Key: $"conflict_{i}", Value: new[] { $"{c.DutyKeyA} vs {c.DutyKeyB}: {c.Reason}" }))
                .ToDictionary(x => x.Key, x => x.Value);
            var envelope = new ApiErrorEnvelope("https://httpstatuses.io/409", "Segregation of Duties conflict", 409, ex.Message, errors);
            return new ObjectResult(envelope) { StatusCode = StatusCodes.Status409Conflict };
        }
    }

    [HttpDelete("{id:guid}/roles/{roleKey}")]
    public async Task<IActionResult> RemoveRole(Guid id, string roleKey, CancellationToken cancellationToken)
    {
        try { return Ok(await _userService.RemoveRoleAsync(id, roleKey, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }
}
