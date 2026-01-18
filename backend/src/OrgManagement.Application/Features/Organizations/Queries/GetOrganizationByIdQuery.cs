using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Exceptions;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Organizations.Queries;

public record GetOrganizationByIdQuery(Guid Id) : IRequest<OrganizationDetailDto>;

public record OrganizationDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? Code,
    OrganizationStatus Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    IEnumerable<SubOrganizationTreeDto> SubOrganizations,
    OrganizationStatsDto Stats);

public record SubOrganizationTreeDto(
    Guid Id,
    string Name,
    string? Description,
    string? Code,
    OrganizationStatus Status,
    int Level,
    Guid? ParentId,
    int UserCount,
    IEnumerable<SubOrganizationTreeDto> Children);

public record OrganizationStatsDto(
    int TotalSubOrganizations,
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers);

public class GetOrganizationByIdQueryHandler : IRequestHandler<GetOrganizationByIdQuery, OrganizationDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrganizationByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrganizationDetailDto> Handle(
        GetOrganizationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .Include(o => o.SubOrganizations.Where(s => !s.IsDeleted))
                .ThenInclude(s => s.Users.Where(u => !u.IsDeleted))
            .Include(o => o.Users.Where(u => !u.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.Id);
        }

        var subOrgsTree = BuildSubOrganizationTree(organization.SubOrganizations, null);

        var stats = new OrganizationStatsDto(
            TotalSubOrganizations: organization.SubOrganizations.Count,
            TotalUsers: organization.Users.Count,
            ActiveUsers: organization.Users.Count(u => u.Status == UserStatus.Active),
            InactiveUsers: organization.Users.Count(u => u.Status != UserStatus.Active));

        return new OrganizationDetailDto(
            Id: organization.Id,
            Name: organization.Name,
            Description: organization.Description,
            Code: organization.Code,
            Status: organization.Status,
            CreatedAt: organization.CreatedAt,
            ModifiedAt: organization.ModifiedAt,
            SubOrganizations: subOrgsTree,
            Stats: stats);
    }

    private static IEnumerable<SubOrganizationTreeDto> BuildSubOrganizationTree(
        IEnumerable<SubOrganization> subOrgs,
        Guid? parentId)
    {
        return subOrgs
            .Where(s => s.ParentSubOrganizationId == parentId)
            .Select(s => new SubOrganizationTreeDto(
                Id: s.Id,
                Name: s.Name,
                Description: s.Description,
                Code: s.Code,
                Status: s.Status,
                Level: s.Level,
                ParentId: s.ParentSubOrganizationId,
                UserCount: s.Users.Count,
                Children: BuildSubOrganizationTree(subOrgs, s.Id)))
            .ToList();
    }
}
