using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Organizations.Queries;

public record GetOrganizationsQuery(
    string? SearchTerm = null,
    OrganizationStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "Name",
    bool SortDescending = false) : IRequest<PaginatedList<OrganizationDto>>;

public record OrganizationDto(
    Guid Id,
    string Name,
    string? Description,
    string? Code,
    OrganizationStatus Status,
    int SubOrganizationCount,
    int UserCount,
    DateTime CreatedAt);

public class GetOrganizationsQueryHandler : IRequestHandler<GetOrganizationsQuery, PaginatedList<OrganizationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetOrganizationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<OrganizationDto>> Handle(
        GetOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Organizations
            .Include(o => o.SubOrganizations)
            .Include(o => o.Users)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(searchTerm) ||
                (o.Code != null && o.Code.ToLower().Contains(searchTerm)));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        query = request.SortBy.ToLower() switch
        {
            "name" => request.SortDescending ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name),
            "code" => request.SortDescending ? query.OrderByDescending(o => o.Code) : query.OrderBy(o => o.Code),
            "status" => request.SortDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            "createdat" => request.SortDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt),
            _ => query.OrderBy(o => o.Name)
        };

        var projectedQuery = query.Select(o => new OrganizationDto(
            o.Id,
            o.Name,
            o.Description,
            o.Code,
            o.Status,
            o.SubOrganizations.Count(s => !s.IsDeleted),
            o.Users.Count(u => !u.IsDeleted),
            o.CreatedAt));

        return await PaginatedList<OrganizationDto>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
