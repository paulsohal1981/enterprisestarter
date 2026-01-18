using MediatR;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Enums;

namespace OrgManagement.Application.Features.Auth.Commands;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public LogoutCommandHandler(
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _tokenService.RevokeTokenAsync(request.RefreshToken, "Logged out by user", cancellationToken);

        if (Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            await _auditService.LogAsync(
                nameof(Domain.Entities.User),
                userId,
                AuditAction.Logout,
                cancellationToken: cancellationToken);
        }

        return Result.Success();
    }
}
