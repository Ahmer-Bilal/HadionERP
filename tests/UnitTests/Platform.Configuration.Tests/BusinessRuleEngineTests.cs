using Platform.Configuration.Rules;

namespace Platform.Configuration.Tests;

public class BusinessRuleEngineTests
{
    [Fact]
    public void Returns_null_when_no_rule_matches()
    {
        var engine = new BusinessRuleEngine();

        Assert.Null(engine.Evaluate("Tax.Determination", new Dictionary<string, string> { ["Country"] = "US" }));
    }

    [Fact]
    public void A_specific_rule_wins_over_a_catch_all_fallback_regardless_of_registration_order()
    {
        var engine = new BusinessRuleEngine();
        // Registered fallback first, specific rule second — priority should still decide the winner.
        engine.Register(new BusinessRule("Tax.Determination", "Default (no VAT)", Condition: null, Outcome: "NoVat", Priority: 0));
        engine.Register(new BusinessRule("Tax.Determination", "Saudi VAT 15%",
            new Dictionary<string, string> { ["Country"] = "SA" }, "VAT15", Priority: 10));

        var saudi = engine.Evaluate("Tax.Determination", new Dictionary<string, string> { ["Country"] = "SA" });
        var other = engine.Evaluate("Tax.Determination", new Dictionary<string, string> { ["Country"] = "US" });

        Assert.Equal("VAT15", saudi);
        Assert.Equal("NoVat", other); // specific rule's condition doesn't match, falls through to catch-all
    }

    [Fact]
    public void Threshold_based_rules_work_the_same_way_as_workflow_step_conditions()
    {
        var engine = new BusinessRuleEngine();
        engine.Register(new BusinessRule("Procurement.ApprovalTier", "Standard", Condition: null, Outcome: "SingleApproval", Priority: 0));
        engine.Register(new BusinessRule("Procurement.ApprovalTier", "Large purchase",
            new Dictionary<string, string> { ["MinAmount"] = "100000" }, "DualApproval", Priority: 10));

        var large = engine.Evaluate("Procurement.ApprovalTier", new Dictionary<string, string> { ["Amount"] = "150000" });
        var small = engine.Evaluate("Procurement.ApprovalTier", new Dictionary<string, string> { ["Amount"] = "5000" });

        Assert.Equal("DualApproval", large);
        Assert.Equal("SingleApproval", small);
    }

    [Fact]
    public void Rules_under_a_different_key_are_never_considered()
    {
        var engine = new BusinessRuleEngine();
        engine.Register(new BusinessRule("Tax.Determination", "Saudi VAT", null, "VAT15"));

        Assert.Null(engine.Evaluate("Posting.AccountDetermination", new Dictionary<string, string>()));
    }
}
