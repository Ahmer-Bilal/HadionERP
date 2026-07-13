using Platform.Core;

namespace Platform.Configuration.Rules;

public sealed class BusinessRuleEngine : IBusinessRuleEngine
{
    private readonly List<BusinessRule> _rules = new();

    public void Register(BusinessRule rule) => _rules.Add(rule);

    public string? Evaluate(string ruleKey, IReadOnlyDictionary<string, string>? context)
    {
        var match = _rules
            .Where(r => r.RuleKey == ruleKey)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault(r => AttributeConstraints.Satisfies(r.Condition, context));

        return match?.Outcome;
    }
}
