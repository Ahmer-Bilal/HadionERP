# Platform.Workflow

Configurable approval-routing engine — condition-based multi-step routing, Any/All quorum, rejection
short-circuit, out-of-office delegation, and SLA/escalation detection. See
docs/architecture/03-platform-services.md #4.

**Deviation from ADR-6 (documented, not silent)**: ADR-6 called for wrapping Elsa Workflows. What's
actually needed at this stage — condition-based sequential/parallel approval routing — doesn't require a
full BPMN engine (gateways, sub-processes, cross-system human tasks), so this is a hand-built engine
behind `IWorkflowEngine`/`IWorkflowDefinitionCatalog`, the same "swap the implementation behind a stable
interface" pattern used everywhere else in the kernel. If genuine BPMN-level complexity is needed later
(parallel gateways spanning multiple systems, long-running sub-processes), Elsa can still be adopted then
behind these same interfaces — no calling code changes.

## What's built
- `WorkflowDefinition`/`WorkflowStepDefinition`: ordered steps, each gated by an optional condition
  (`AttributeConstraints` — shared with Platform.Security's ABAC grants) so a step only applies for
  matching resource context (e.g. a second approver only for amounts over a threshold).
- `WorkflowInstance`: owns its own Running/Approved/Rejected/Cancelled lifecycle (distinct from a Business
  Object's own lifecycle in Platform.Core), with Any-quorum (first eligible approval decides) and
  All-quorum (every named approver must approve) support.
- `RoleBasedWorkflowEligibilityService` + `Delegation/`: a principal can act on a step by holding its
  required Role directly (Platform.Security) or via an active out-of-office delegation.
- `Escalation/EscalationScan`: pure query finding running instances whose current step has exceeded its
  configured SLA hours.
- `IWorkflowInstanceRepository`: the persistence port for `WorkflowInstance` — added when Modules.MasterData
  became the first real consumer of this engine (a running approval can span separate HTTP requests, e.g.
  submit today, decide days later, so it must survive somewhere real between them). Platform.Workflow
  itself stays storage-agnostic on purpose (the same "kernel defines the port, a module with a real
  database implements it" pattern as `Platform.Core.NumberRanges.INumberRangeService`) — the actual
  implementation lives in `Modules.MasterData.Infrastructure.EfWorkflowInstanceRepository` since that's the
  only module with a real database so far. `WorkflowInstance` has a private parameterless constructor
  reserved for this (ORM materialization), mirroring `Platform.Core.BusinessObject`'s own pattern.

## First real consumer: Modules.MasterData's Business Partner onboarding approval
`BusinessPartnerWorkflow.SubmitApprovalDefinition` (in `Modules.MasterData.Application`) is the first real
`WorkflowDefinition` registered into the catalog — one Any-quorum step
(`MasterData.ApproveBusinessPartner`). `BusinessPartnerService.SubmitAsync` starts an instance; `ApproveAsync`/
`RejectAsync` decide it, and only apply the underlying Business Partner's own Approved/Rejected transition
once the *workflow* reaches that final state — replacing what used to be a direct, unconditional call to
`BusinessPartner.Approve()`. See `Modules.MasterData/README.md` for the full story, including the disclosed
temporary shim (every actor is granted the approver role unconditionally, since real Security/role
assignment isn't wired yet — the very next slice).

## Deferred
- **The actual scheduler** that calls `EscalationScan` periodically and notifies the escalation target —
  needs a hosted background service in Gateway.Api, not built yet (the logic it will call is built and
  tested now).
- **Notification delivery** (email/SMS/in-app) on step decisions — a side effect of
  `Events.WorkflowStepDecidedEvent`/`WorkflowCompletedEvent`. Platform.Events exists and is wired for other
  things (e.g. `ApplicationStarted`), but nothing subscribes to these two workflow events yet — same gap
  `BusinessObject`'s own `DomainEvents` have (never published either). A future slice, not this one.
- Every module beyond Modules.MasterData still owns no workflow definitions of its own (e.g. Procurement's
  real "PurchaseOrder.Submit" approval matrix) — register them into the same catalog when that module is
  built, the same way Modules.MasterData did here.
