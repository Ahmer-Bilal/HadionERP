namespace Platform.Security.Sod;

public sealed class SodEngine : ISodEngine
{
    private readonly IReadOnlyCollection<SodConflictRule> _rules;
    private readonly ISodExceptionLog _exceptionLog;

    public SodEngine(IEnumerable<SodConflictRule> rules, ISodExceptionLog exceptionLog)
    {
        _rules = rules.ToList();
        _exceptionLog = exceptionLog;
    }

    public IReadOnlyCollection<SodConflictRule> FindConflicts(IReadOnlyCollection<string> dutyKeys)
    {
        var keys = dutyKeys.ToHashSet();
        return _rules.Where(rule => keys.Contains(rule.DutyKeyA) && keys.Contains(rule.DutyKeyB)).ToList();
    }

    public IReadOnlyCollection<SodConflictRule> FindUnresolvedConflicts(string userId, IReadOnlyCollection<string> dutyKeys)
    {
        return FindConflicts(dutyKeys)
            .Where(rule => !_exceptionLog.IsGranted(userId, rule))
            .ToList();
    }
}
