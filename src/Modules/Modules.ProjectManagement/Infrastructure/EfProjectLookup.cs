using Microsoft.EntityFrameworkCore;
using Modules.ProjectManagement.Contracts;

namespace Modules.ProjectManagement.Infrastructure;

/// <summary>Implements <see cref="IProjectLookup"/> against this module's own
/// <see cref="ProjectManagementDbContext"/> — read-only, no tracking, same pattern as
/// <c>Modules.MasterData.Infrastructure.EfBusinessPartnerLookup</c>.</summary>
public sealed class EfProjectLookup : IProjectLookup
{
    private readonly ProjectManagementDbContext _dbContext;

    public EfProjectLookup(ProjectManagementDbContext dbContext) => _dbContext = dbContext;

    public async Task<ProjectSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects.AsNoTracking().Include(p => p.WbsElements)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null) return null;

        return new ProjectSummary(
            project.Id, project.DocumentNumber, project.ProjectName, project.ProjectNameArabic,
            project.CustomerId, project.Status.ToString(),
            project.WbsElements.Select(w => new WbsElementSummary(
                w.Id, w.Code, w.Name, w.NameArabic, w.IsPlanningElement, w.IsAccountAssignmentElement, w.IsBillingElement)).ToList());
    }
}
