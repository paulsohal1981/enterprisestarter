using OrgManagement.Domain.Entities;

namespace OrgManagement.Application.Common.Interfaces;

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(User user, CancellationToken cancellationToken = default);
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string refreshToken, string reason, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}
