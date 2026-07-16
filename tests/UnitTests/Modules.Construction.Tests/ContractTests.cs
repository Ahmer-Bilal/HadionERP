using Modules.Construction.Domain;
using Platform.Core;

namespace Modules.Construction.Tests;

public class ContractTests
{
    private static readonly Guid WbsElementId = Guid.NewGuid();

    [Fact]
    public void A_new_contract_starts_in_draft_with_no_document_number()
    {
        var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);

        Assert.Equal(BusinessObjectStatus.Draft, contract.Status);
        Assert.Null(contract.DocumentNumber);
        Assert.Empty(contract.BoqLines);
        Assert.Equal(0m, contract.ContractValue);
    }

    [Fact]
    public void Blank_contract_type_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Contract("ahmer.bilal", Guid.NewGuid(), "", null, null, null));
    }

    [Fact]
    public void Advance_payment_percentage_outside_0_to_100_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, 150m, null));
    }

    [Fact]
    public void AddBoqLine_computes_amount_and_rolls_up_contract_value()
    {
        var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);
        var line = contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsElementId);

        Assert.Equal(5000m, line.Amount);
        Assert.Equal(5000m, contract.ContractValue);
    }

    [Fact]
    public void AddBoqLine_rejects_a_duplicate_code_within_the_same_contract()
    {
        var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsElementId);
        Assert.Throws<ArgumentException>(() => contract.AddBoqLine("BOQ-001", "Duplicate", null, "M3", 10m, 5m, WbsElementId));
    }

    [Fact]
    public void AddBoqLine_rejects_zero_or_negative_quantity_or_rate()
    {
        var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);
        Assert.Throws<ArgumentException>(() => contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 0m, 50m, WbsElementId));
        Assert.Throws<ArgumentException>(() => contract.AddBoqLine("BOQ-002", "Excavation", null, "M3", 100m, -1m, WbsElementId));
    }

    [Fact]
    public void AddBoqLine_after_submit_is_rejected()
    {
        var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsElementId);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contract.Submit("ahmer.bilal");

        Assert.Throws<InvalidOperationException>(() => contract.AddBoqLine("BOQ-002", "Backfill", null, "M3", 10m, 5m, WbsElementId));
    }

    [Fact]
    public void Full_lifecycle_draft_to_submitted_to_approved()
    {
        var contract = new Contract("ahmer.bilal", Guid.NewGuid(), "LumpSum", null, null, null);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsElementId);
        contract.AssignNumber("CON-CONTR-2026-000001");

        contract.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, contract.Status);

        contract.Approve("con.manager");
        Assert.Equal(BusinessObjectStatus.Approved, contract.Status);
    }
}
