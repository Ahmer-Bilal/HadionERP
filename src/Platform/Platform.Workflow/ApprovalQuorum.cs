namespace Platform.Workflow;

/// <summary>How many eligible approvers a step needs before it's satisfied.</summary>
public enum ApprovalQuorum
{
    /// <summary>The first eligible approver's decision decides the step (the common case).</summary>
    Any,

    /// <summary>Every approver named for this step instance must approve — any single rejection
    /// rejects the whole workflow.</summary>
    All
}
