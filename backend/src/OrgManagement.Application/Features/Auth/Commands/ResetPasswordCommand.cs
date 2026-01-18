using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Auth.Commands;

public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService,
        IAuditService auditService)
    {
        _context = context;
        _passwordService = passwordService;
        _auditService = auditService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user == null || !user.ValidatePasswordResetToken(request.Token))
        {
            return Result.Failure("Invalid or expired password reset token.");
        }

        var newPasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.SetPassword(newPasswordHash);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(Domain.Entities.User),
            user.Id,
            AuditAction.PasswordChange,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
