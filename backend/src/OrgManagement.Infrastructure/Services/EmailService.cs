using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{baseUrl}/auth/reset-password?token={resetToken}";

        // TODO: Implement actual email sending (SMTP, SendGrid, etc.)
        _logger.LogInformation(
            "Password reset email would be sent to {Email} with URL: {ResetUrl}",
            email,
            resetUrl);

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string temporaryPassword, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual email sending
        _logger.LogInformation(
            "Welcome email would be sent to {Email} with temporary password: {Password}",
            email,
            temporaryPassword);

        return Task.CompletedTask;
    }
}
