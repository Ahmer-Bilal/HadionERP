using Platform.Security.Sod;

namespace Modules.Identity.Application;

/// <summary>Thrown by <see cref="UserService.AssignRoleAsync"/> when the proposed role set has an
/// unresolved Segregation of Duties conflict and no override reason was supplied — the Api layer maps this
/// to a 409 carrying the conflicting rules, per the real SAP GRC "risk acceptance" pattern this mirrors.</summary>
public sealed class SodConflictException : Exception
{
    public IReadOnlyCollection<SodConflictRule> Conflicts { get; }

    public SodConflictException(IReadOnlyCollection<SodConflictRule> conflicts)
        : base("Assigning this role would create an unresolved Segregation of Duties conflict.")
    {
        Conflicts = conflicts;
    }
}
