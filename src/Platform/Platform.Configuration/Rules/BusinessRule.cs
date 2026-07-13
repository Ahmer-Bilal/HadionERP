namespace Platform.Configuration.Rules;

/// <summary>
/// One row of a decision table (docs/architecture/04-data-and-api.md #3.3: "rules that are naturally
/// declarative... expressed in a rules table interpreted at runtime, not compiled into module code") —
/// e.g. tax determination, posting-account determination, or a validation rule. Several rules can share
/// the same <paramref name="RuleKey"/>, each covering a different <paramref name="Condition"/>
/// (evaluated with the same attribute-constraint logic as Platform.Security's ABAC grants and
/// Platform.Workflow's step conditions); <paramref name="Priority"/> breaks ties when more than one rule
/// could apply — higher evaluates first, so a specific rule can be checked before a catch-all fallback
/// (a fallback rule typically has no Condition and Priority 0, matching everything last).
/// </summary>
public sealed record BusinessRule(string RuleKey, string Description, IReadOnlyDictionary<string, string>? Condition, string Outcome, int Priority = 0);
