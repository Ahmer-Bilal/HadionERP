using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Platform.Security;

namespace Modules.Identity.Infrastructure;

/// <summary>
/// JWT implementation of <see cref="ITokenService"/> — see that interface's own doc comment for why it's
/// defined in Platform.Security but implemented here. Short-lived, symmetrically-signed tokens; no refresh-
/// token rotation in this pass (`ARCHITECTURE-AUDIT.md` Part 1 §1's own disclosed scope boundary) — a
/// token's holder simply re-authenticates after it expires.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    /// <summary>Public so Gateway.Api's <c>Program.cs</c> can configure the JWT bearer validation
    /// parameters with the exact same values this class signs with, instead of duplicating the literals.</summary>
    public const string Issuer = "HadionERP";
    public const string Audience = "HadionERP.Api";

    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(string signingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingKey);
        if (signingKey.Length < 32)
            throw new ArgumentException("The JWT signing key must be at least 32 characters.", nameof(signingKey));
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }

    public (string Token, DateTimeOffset ExpiresAt) IssueToken(string username, IReadOnlyCollection<string> roleKeys, TimeSpan lifetime)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        var claims = new List<Claim> { new(ClaimTypes.Name, username) };
        claims.AddRange(roleKeys.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
