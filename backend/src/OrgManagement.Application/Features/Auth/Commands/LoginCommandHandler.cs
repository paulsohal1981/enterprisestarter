using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService,
        IAuditService auditService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _auditService = auditService;
        _configuration = configuration;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            return Result.Failure<LoginResponse>("Invalid email or password.");
        }

        if (user.IsLockedOut)
        {
            return Result.Failure<LoginResponse>("Account is locked. Please try again later or contact support.");
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<LoginResponse>("Account is not active. Please contact support.");
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordLoginFailure();
            await _context.SaveChangesAsync(cancellationToken);

            if (user.IsLockedOut)
            {
                return Result.Failure<LoginResponse>("Account has been locked due to multiple failed login attempts.");
            }

            return Result.Failure<LoginResponse>("Invalid email or password.");
        }

        // Successful login
        user.RecordLoginSuccess();
        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Log the login
        await _auditService.LogAsync(
            nameof(Domain.Entities.User),
            user.Id,
            AuditAction.Login,
            cancellationToken: cancellationToken);

        var expirationMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        return Result.Success(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expirationMinutes),
            User: new UserInfo(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                OrganizationId: user.OrganizationId,
                SubOrganizationId: user.SubOrganizationId,
                MustChangePassword: user.MustChangePassword,
                Roles: roles,
                Permissions: permissions)));
    }
}
