using Modules.ProjectManagement.Domain;
using Platform.Core;

namespace Modules.ProjectManagement.Tests;

public class ProjectTests
{
    [Fact]
    public void A_new_project_starts_in_draft_with_no_document_number()
    {
        var project = new Project("ahmer.bilal", "Tower A Construction", null, null, null, null);

        Assert.Equal(BusinessObjectStatus.Draft, project.Status);
        Assert.Null(project.DocumentNumber);
        Assert.Empty(project.WbsElements);
    }

    [Fact]
    public void Blank_project_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Project("ahmer.bilal", "", null, null, null, null));
    }

    [Fact]
    public void AddWbsElement_builds_a_top_level_element()
    {
        var project = new Project("ahmer.bilal", "Tower A", null, null, null, null);
        var element = project.AddWbsElement("1.0", "Civil Works", null, null, true, true, false);

        Assert.Single(project.WbsElements);
        Assert.Null(element.ParentWbsElementId);
        Assert.True(element.IsPlanningElement);
        Assert.True(element.IsAccountAssignmentElement);
        Assert.False(element.IsBillingElement);
    }

    [Fact]
    public void AddWbsElement_builds_a_child_referencing_an_already_added_parent()
    {
        var project = new Project("ahmer.bilal", "Tower A", null, null, null, null);
        var root = project.AddWbsElement("1.0", "Civil Works", null, null, false, false, false);
        var child = project.AddWbsElement("1.1", "Foundation", null, root.Id, true, true, false);

        Assert.Equal(root.Id, child.ParentWbsElementId);
        Assert.Equal(2, project.WbsElements.Count);
    }

    [Fact]
    public void AddWbsElement_rejects_a_parent_that_does_not_belong_to_this_project()
    {
        var project = new Project("ahmer.bilal", "Tower A", null, null, null, null);
        Assert.Throws<ArgumentException>(() => project.AddWbsElement("1.1", "Foundation", null, Guid.NewGuid(), true, true, false));
    }

    [Fact]
    public void AddWbsElement_rejects_a_duplicate_code_within_the_same_project()
    {
        var project = new Project("ahmer.bilal", "Tower A", null, null, null, null);
        project.AddWbsElement("1.0", "Civil Works", null, null, false, false, false);
        Assert.Throws<ArgumentException>(() => project.AddWbsElement("1.0", "Duplicate", null, null, false, false, false));
    }

    [Fact]
    public void AddWbsElement_after_submit_is_rejected()
    {
        var project = new Project("ahmer.bilal", "Tower A", null, null, null, null);
        project.AddWbsElement("1.0", "Civil Works", null, null, true, true, false);
        project.AssignNumber("PM-PRJ-2026-000001");
        project.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => project.AddWbsElement("2.0", "MEP Works", null, null, true, true, false));
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var project = new Project("ahmer.bilal", "Tower A", null, null, null, null);
        project.AddWbsElement("1.0", "Civil Works", null, null, true, true, false);
        project.AssignNumber("PM-PRJ-2026-000001");

        project.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, project.Status);

        project.Approve("pm.manager");
        Assert.Equal(BusinessObjectStatus.Approved, project.Status);
    }
}
