using Platform.Core;
using Platform.Core.Events;
using Platform.Core.Lifecycle;
using Platform.Core.NumberRanges;

namespace Platform.Core.Tests;

/// <summary>
/// Proves the Phase 0 platform kernel exit criteria end-to-end on a demo BO with no business meaning:
/// create → number → submit → approve → post → reverse, with domain events at each step, extension
/// fields, relations, and the "no hard delete past Draft" rule all working — before any real module
/// exists. See docs/architecture/06-roadmap.md, Phase 0.
/// </summary>
public class PhaseZeroExitCriteriaTests
{
    [Fact]
    public void New_business_object_starts_in_draft_with_no_number()
    {
        var bo = new DemoBusinessObject("ahmer.bilal", 1000m);

        Assert.Equal(BusinessObjectStatus.Draft, bo.Status);
        Assert.Null(bo.DocumentNumber);
        Assert.True(bo.CanHardDelete);
        Assert.Empty(bo.DomainEvents);
    }

    [Fact]
    public void Number_range_service_assigns_sequential_formatted_numbers()
    {
        var service = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition("DEMO-DOC", "DEMO", "DOC")
        });

        var first = service.GetNext("DEMO-DOC", "C001", 2026);
        var second = service.GetNext("DEMO-DOC", "C001", 2026);

        Assert.Equal("DEMO-DOC-2026-000001", first);
        Assert.Equal("DEMO-DOC-2026-000002", second);
    }

    [Fact]
    public void Number_range_counters_are_isolated_per_company_and_fiscal_year()
    {
        var service = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition("DEMO-DOC", "DEMO", "DOC")
        });

        var companyA = service.GetNext("DEMO-DOC", "C001", 2026);
        var companyB = service.GetNext("DEMO-DOC", "C002", 2026);
        var nextYear = service.GetNext("DEMO-DOC", "C001", 2027);

        Assert.Equal("DEMO-DOC-2026-000001", companyA);
        Assert.Equal("DEMO-DOC-2026-000001", companyB);
        Assert.Equal("DEMO-DOC-2027-000001", nextYear);
    }

    [Fact]
    public void Full_lifecycle_create_submit_approve_post_reverse_succeeds_and_raises_events()
    {
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition("DEMO-DOC", "DEMO", "DOC")
        });

        var bo = new DemoBusinessObject("ahmer.bilal", 5000m);
        bo.AssignNumber(numberRanges.GetNext("DEMO-DOC", "C001", 2026));

        bo.Transition(BusinessObjectTransition.Submit, "ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, bo.Status);

        bo.Transition(BusinessObjectTransition.StartApproval, "workflow-engine");
        Assert.Equal(BusinessObjectStatus.InApproval, bo.Status);

        bo.Transition(BusinessObjectTransition.Approve, "finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, bo.Status);

        bo.Transition(BusinessObjectTransition.Post, "finance.manager");
        Assert.Equal(BusinessObjectStatus.Posted, bo.Status);
        Assert.False(bo.CanHardDelete, "a posted document must never be hard-deletable");

        bo.Transition(BusinessObjectTransition.Reverse, "finance.manager");
        Assert.Equal(BusinessObjectStatus.Reversed, bo.Status);

        Assert.Equal(5, bo.DomainEvents.Count);
        Assert.All(bo.DomainEvents, e => Assert.IsType<BusinessObjectStatusChangedEvent>(e));

        var events = bo.DomainEvents.Cast<BusinessObjectStatusChangedEvent>().ToList();
        Assert.Equal(BusinessObjectTransition.Submit, events[0].Transition);
        Assert.Equal(BusinessObjectTransition.Reverse, events[^1].Transition);
        Assert.Equal(BusinessObjectStatus.Posted, events[^1].From);
        Assert.Equal(BusinessObjectStatus.Reversed, events[^1].To);
        Assert.All(events, e => Assert.Equal(nameof(DemoBusinessObject), e.BusinessObjectType));

        Assert.Equal(5, bo.RowVersion);
    }

    [Fact]
    public void Illegal_transition_is_rejected_by_the_kernel()
    {
        var bo = new DemoBusinessObject("ahmer.bilal", 100m);

        var ex = Assert.Throws<InvalidLifecycleTransitionException>(
            () => bo.Transition(BusinessObjectTransition.Post, "ahmer.bilal"));

        Assert.Contains("Post", ex.Message);
        Assert.Contains("Draft", ex.Message);
    }

    [Fact]
    public void Business_rule_guard_can_reject_a_structurally_legal_transition()
    {
        var bo = new DemoBusinessObject("ahmer.bilal", 250_000m);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            bo.Transition(BusinessObjectTransition.Submit, "ahmer.bilal", guard: () => bo.Amount <= 100_000m));

        Assert.Contains("guard rejected", ex.Message);
        Assert.Equal(BusinessObjectStatus.Draft, bo.Status);
        Assert.Empty(bo.DomainEvents);
    }

    [Fact]
    public void Cannot_assign_a_document_number_twice()
    {
        var bo = new DemoBusinessObject("ahmer.bilal", 100m);
        bo.AssignNumber("DEMO-DOC-2026-000001");

        Assert.Throws<InvalidOperationException>(() => bo.AssignNumber("DEMO-DOC-2026-000002"));
    }

    [Fact]
    public void Extension_fields_round_trip_through_json_without_a_schema_change()
    {
        var bo = new DemoBusinessObject("ahmer.bilal", 100m);

        bo.ExtensionFields.Set("siteEngineerName", "Khalid Al-Otaibi");
        bo.ExtensionFields.Set("requiresSafetyInspection", true);
        bo.ExtensionFields.Set("inspectionScore", 87);

        var json = bo.ExtensionFields.ToJson();
        var rehydrated = ExtensionFieldBag.FromJson(json);

        Assert.Equal("Khalid Al-Otaibi", rehydrated.Get<string>("siteEngineerName"));
        Assert.True(rehydrated.Get<bool>("requiresSafetyInspection"));
        Assert.Equal(87, rehydrated.Get<int>("inspectionScore"));
        Assert.False(rehydrated.Has("fieldThatWasNeverSet"));
    }

    [Fact]
    public void Relations_link_one_business_object_to_another_for_related_document_navigation()
    {
        var sourceId = Guid.NewGuid();
        var bo = new DemoBusinessObject("ahmer.bilal", 100m);

        bo.AddRelation(new BusinessObjectReference(sourceId, "DemoRequestForQuotation", "CreatedFrom"));

        var relation = Assert.Single(bo.Relations);
        Assert.Equal(sourceId, relation.TargetId);
        Assert.Equal("CreatedFrom", relation.RelationKind);
    }
}
