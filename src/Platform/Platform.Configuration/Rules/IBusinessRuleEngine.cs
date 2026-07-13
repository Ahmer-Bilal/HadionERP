namespace Platform.Configuration.Rules;

public interface IBusinessRuleEngine
{
    void Register(BusinessRule rule);

    /// <summary>Evaluates all rules registered under <paramref name="ruleKey"/>, highest priority first,
    /// returning the first one whose condition is satisfied by <paramref name="context"/> — or null if
    /// none match, meaning the caller applies its own default behavior.</summary>
    string? Evaluate(string ruleKey, IReadOnlyDictionary<string, string>? context);
}
