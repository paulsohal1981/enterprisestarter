using MediatR;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;

namespace OrgManagement.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;

public record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _tokenService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (result == null)
        {
            return Result.Failure<RefreshTokenResponse>("Invalid or expired refresh token.");
        }

        return Result.Success(new RefreshTokenResponse(
            AccessToken: result.Value.AccessToken,
            RefreshToken: result.Value.RefreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(60)));
    }
}
