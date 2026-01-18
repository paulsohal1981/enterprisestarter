namespace OrgManagement.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string email, string temporaryPassword, CancellationToken cancellationToken = default);
}
