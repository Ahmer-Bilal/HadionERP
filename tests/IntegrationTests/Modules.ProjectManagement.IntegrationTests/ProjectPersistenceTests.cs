using Microsoft.EntityFrameworkCore;
using Modules.ProjectManagement.Domain;
using Platform.Core;
using Xunit;

namespace Modules.ProjectManagement.IntegrationTests;

public class ProjectPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_project_with_a_wbs_hierarchy_reads_back_identically()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var project = new Project("ahmer.bilal", "Tower A Construction", "برج أ", Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));
            var root = project.AddWbsElement("1.0", "Civil Works", null, null, false, false, false);
            project.AddWbsElement("1.1", "Foundation", null, root.Id, true, true, false);
            project.AssignNumber("PM-PRJ-2026-000001");
            writeContext.Projects.Add(project);
            await writeContext.SaveChangesAsync();

            project.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            id = project.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Projects.Include(p => p.WbsElements).FirstOrDefaultAsync(p => p.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("PM-PRJ-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Submitted, reloaded.Status);
        Assert.Equal("برج أ", reloaded.ProjectNameArabic);
        Assert.Equal(2, reloaded.WbsElements.Count);
        var reloadedRoot = reloaded.WbsElements.Single(w => w.Code == "1.0");
        var reloadedChild = reloaded.WbsElements.Single(w => w.Code == "1.1");
        Assert.Null(reloadedRoot.ParentWbsElementId);
        Assert.Equal(reloadedRoot.Id, reloadedChild.ParentWbsElementId);
        Assert.True(reloadedChild.IsPlanningElement);
        Assert.True(reloadedChild.IsAccountAssignmentElement);
    }

    [Fact]
    public async Task Deleting_a_project_cascades_to_its_wbs_elements()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var project = new Project("ahmer.bilal", "Tower B", null, null, null, null);
            project.AddWbsElement("1.0", "Civil Works", null, null, true, true, false);
            project.AssignNumber("PM-PRJ-2026-000002");
            writeContext.Projects.Add(project);
            await writeContext.SaveChangesAsync();
            id = project.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var project = await deleteContext.Projects.FirstAsync(p => p.Id == id);
            deleteContext.Projects.Remove(project);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remaining = await readContext.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM projectmanagement.wbs_elements").SingleAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task RowVersion_increments_across_real_transitions()
    {
        await using var context = TestDatabase.CreateContext();
        var project = new Project("ahmer.bilal", "Tower C", null, null, null, null);
        project.AddWbsElement("1.0", "Civil Works", null, null, true, true, false);
        project.AssignNumber("PM-PRJ-2026-000003");
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var afterCreate = project.RowVersion;

        project.Submit("ahmer.bilal");
        await context.SaveChangesAsync();
        var afterSubmit = project.RowVersion;

        project.Approve("pm.manager");
        await context.SaveChangesAsync();
        var afterApprove = project.RowVersion;

        Assert.NotEqual(afterCreate, afterSubmit);
        Assert.NotEqual(afterSubmit, afterApprove);
    }
}
