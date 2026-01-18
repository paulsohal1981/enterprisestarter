using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Exceptions;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDetailDto>;

public record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserStatus Status,
    bool MustChangePassword,
    Guid OrganizationId,
    string OrganizationName,
    Guid? SubOrganizationId,
    string? SubOrganizationName,
    IEnumerable<UserRoleDto> Roles,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    DateTime? LastLoginAt);

public record UserRoleDto(Guid Id, string Name, bool IsSystemRole);

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDetailDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.SubOrganization)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        return new UserDetailDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Status: user.Status,
            MustChangePassword: user.MustChangePassword,
            OrganizationId: user.OrganizationId,
            OrganizationName: user.Organization.Name,
            SubOrganizationId: user.SubOrganizationId,
            SubOrganizationName: user.SubOrganization?.Name,
            Roles: user.UserRoles.Select(ur => new UserRoleDto(ur.Role.Id, ur.Role.Name, ur.Role.IsSystemRole)),
            CreatedAt: user.CreatedAt,
            ModifiedAt: user.ModifiedAt,
            LastLoginAt: user.LastLoginAt);
    }
}
