using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Users.Queries;

public record GetUsersQuery(
    string? SearchTerm = null,
    UserStatus? Status = null,
    Guid? OrganizationId = null,
    Guid? SubOrganizationId = null,
    Guid? RoleId = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "LastName",
    bool SortDescending = false) : IRequest<PaginatedList<UserDto>>;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserStatus Status,
    Guid OrganizationId,
    string OrganizationName,
    Guid? SubOrganizationId,
    string? SubOrganizationName,
    IEnumerable<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .Include(u => u.Organization)
            .Include(u => u.SubOrganization)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchTerm) ||
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        if (request.OrganizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == request.OrganizationId.Value);
        }

        if (request.SubOrganizationId.HasValue)
        {
            query = query.Where(u => u.SubOrganizationId == request.SubOrganizationId.Value);
        }

        if (request.RoleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == request.RoleId.Value));
        }

        query = request.SortBy.ToLower() switch
        {
            "email" => request.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "firstname" => request.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "lastname" => request.SortDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
            "status" => request.SortDescending ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
            "createdat" => request.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
        };

        var projectedQuery = query.Select(u => new UserDto(
            u.Id,
            u.Email,
            u.FirstName,
            u.LastName,
            u.PhoneNumber,
            u.Status,
            u.OrganizationId,
            u.Organization.Name,
            u.SubOrganizationId,
            u.SubOrganization != null ? u.SubOrganization.Name : null,
            u.UserRoles.Select(ur => ur.Role.Name),
            u.CreatedAt,
            u.LastLoginAt));

        return await PaginatedList<UserDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
