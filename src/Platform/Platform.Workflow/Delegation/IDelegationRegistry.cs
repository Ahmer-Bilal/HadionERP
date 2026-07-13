namespace Platform.Workflow.Delegation;

public interface IDelegationRegistry
{
    void Register(Delegation delegation);

    /// <summary>True if <paramref name="candidateUserId"/> has received a delegation for
    /// <paramref name="roleKey"/> that covers <paramref name="onDate"/> — from anyone, since eligibility
    /// only cares whether the candidate may act, not who specifically delegated to them.</summary>
    bool HasActiveDelegation(string candidateUserId, string roleKey, DateOnly onDate);
}
