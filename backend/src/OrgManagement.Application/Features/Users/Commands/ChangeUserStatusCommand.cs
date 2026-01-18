using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Exceptions;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Users.Commands;

public record ChangeUserStatusCommand(Guid Id, UserStatus Status) : IRequest<Result>;

public class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;

    public ChangeUserStatusCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IAuditService auditService)
    {
        _context = context;
        _tokenService = tokenService;
        _auditService = auditService;
    }

    public async Task<Result> Handle(ChangeUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        var oldStatus = user.Status;

        switch (request.Status)
        {
            case UserStatus.Active:
                user.Activate();
                break;
            case UserStatus.Inactive:
                user.Deactivate();
                await _tokenService.RevokeAllUserTokensAsync(user.Id, "User deactivated", cancellationToken);
                break;
            case UserStatus.Locked:
                user.Deactivate();
                await _tokenService.RevokeAllUserTokensAsync(user.Id, "User locked", cancellationToken);
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(User),
            user.Id,
            request.Status == UserStatus.Active ? AuditAction.Activate : AuditAction.Deactivate,
            oldValues: new { Status = oldStatus },
            newValues: new { Status = request.Status },
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
{
    public ChangeUserStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.");
    }
}
