using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Identity.Application;
using Platform.Api;
using Platform.Security;

namespace Modules.Identity.Api;

/// <summary>
/// The login surface — the one controller in the whole solution that must stay reachable without a token
/// already in hand (<see cref="AllowAnonymous"/> on <see cref="Login"/> only; every other action, including
/// <see cref="Me"/>, still requires a valid bearer token like every other controller now does by default).
/// </summary>
[Route("api/v1/identity/auth")]
public sealed class AuthController : PlatformApiController
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    private readonly UserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(UserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.AuthenticateAsync(request.Username, request.Password, cancellationToken);
        if (user is null) return Unauthorized(ApiErrorEnvelope.Forbidden("Invalid username or password."));

        var (token, expiresAt) = _tokenService.IssueToken(user.Username, user.RoleKeys, TokenLifetime);
        return Ok(new LoginResponse(token, expiresAt, user));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await _userService.GetByUsernameAsync(CurrentActor, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }
}
