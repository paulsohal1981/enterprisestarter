using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Domain.Entities;
using OrgManagement.Infrastructure.Data;

namespace OrgManagement.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public TokenService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync(cancellationToken);

        var roles = userRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = userRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var accessToken = GenerateAccessToken(user, roles, permissions);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, cancellationToken);

        return (accessToken, refreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            return null;
        }

        // Revoke old token
        storedToken.Revoke("Replaced by new token");

        // Generate new tokens
        var newAccessToken = await GenerateAccessTokenForUserAsync(storedToken.User, cancellationToken);
        var newRefreshToken = await GenerateRefreshTokenAsync(storedToken.UserId, cancellationToken);

        storedToken.Revoke("Replaced by new token", newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return (newAccessToken, newRefreshToken);
    }

    public async Task RevokeTokenAsync(
        string refreshToken,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken != null && storedToken.IsActive)
        {
            storedToken.Revoke(reason);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllUserTokensAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private string GenerateAccessToken(User user, List<string> roles, List<string> permissions)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "OrgManagement";
        var audience = jwtSettings["Audience"] ?? "OrgManagement";
        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new("organization_id", user.OrganizationId.ToString()),
            new("must_change_password", user.MustChangePassword.ToString().ToLower())
        };

        if (user.SubOrganizationId.HasValue)
        {
            claims.Add(new Claim("sub_organization_id", user.SubOrganizationId.Value.ToString()));
        }

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateAccessTokenForUserAsync(User user, CancellationToken cancellationToken)
    {
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync(cancellationToken);

        var roles = userRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = userRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        return GenerateAccessToken(user, roles, permissions);
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var refreshTokenExpirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");

        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = RefreshToken.Create(
            userId,
            token,
            DateTime.UtcNow.AddDays(refreshTokenExpirationDays));

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return token;
    }
}
