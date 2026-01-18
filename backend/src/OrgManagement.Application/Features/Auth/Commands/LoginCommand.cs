using MediatR;
using OrgManagement.Application.Common.Models;

namespace OrgManagement.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid OrganizationId,
    Guid? SubOrganizationId,
    bool MustChangePassword,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions);
