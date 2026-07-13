namespace Platform.Workflow.Delegation;

public sealed class InMemoryDelegationRegistry : IDelegationRegistry
{
    private readonly List<Delegation> _delegations = new();

    public void Register(Delegation delegation) => _delegations.Add(delegation);

    public bool HasActiveDelegation(string candidateUserId, string roleKey, DateOnly onDate) =>
        _delegations.Any(d => d.ToUserId == candidateUserId && d.RoleKey == roleKey && d.CoversDate(onDate));
}
