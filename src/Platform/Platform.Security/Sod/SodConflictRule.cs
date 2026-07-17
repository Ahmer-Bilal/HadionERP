namespace Platform.Security.Sod;

/// <summary>
/// A pair of Duties that must not both be held by the same user without a logged exception — e.g.
/// "Create Vendor" and "Approve Vendor Payment" (docs/architecture/04-platform-services.md #2.2). Rules
/// are symmetric: order doesn't matter.
/// </summary>
public sealed record SodConflictRule(string DutyKeyA, string DutyKeyB, string Reason)
{
    public bool Matches(string dutyKeyA, string dutyKeyB) =>
        (DutyKeyA == dutyKeyA && DutyKeyB == dutyKeyB) ||
        (DutyKeyA == dutyKeyB && DutyKeyB == dutyKeyA);
}
