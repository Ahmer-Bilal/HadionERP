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

## Deferred
- **The actual scheduler** that calls `EscalationScan` periodically and notifies the escalation target —
  needs a hosted background service in Gateway.Api, not built yet (the logic it will call is built and
  tested now).
- **Notification delivery** (email/SMS/in-app) on step decisions — a side effect of
  `Events.WorkflowStepDecidedEvent`/`WorkflowCompletedEvent`, wired once Platform.Events (the next Phase 0
  piece) exists to publish them.
- **No workflow definitions are registered yet** — there's no real business module (e.g. Procurement) to
  own a real "PurchaseOrder.Submit" approval matrix yet. `Gateway.Api` wires the engine with an empty
  catalog; a module registers its own definitions into the same catalog when it's built.
