namespace Platform.Security.Sod;

/// <summary>
/// Checks a set of held Duties (e.g. from a role assignment about to be saved, or a principal's current
/// effective Duties) against the registered conflict rules.
/// </summary>
public interface ISodEngine
{
    /// <summary>All conflicts present in <paramref name="dutyKeys"/>, ignoring any granted exceptions —
    /// use this when validating a proposed role assignment before it's saved.</summary>
    IReadOnlyCollection<SodConflictRule> FindConflicts(IReadOnlyCollection<string> dutyKeys);

    /// <summary>Conflicts in <paramref name="dutyKeys"/> that do NOT have a logged exception for
    /// <paramref name="userId"/> — use this to decide whether to actually block the user.</summary>
    IReadOnlyCollection<SodConflictRule> FindUnresolvedConflicts(string userId, IReadOnlyCollection<string> dutyKeys);
}
