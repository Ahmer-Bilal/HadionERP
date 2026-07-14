# Platform.Core

Business Object base classes, lifecycle FSM, number ranges, extension-field storage. See docs/architecture/02-business-object-model.md.

## What "a Business Object" actually guarantees, and where each guarantee lives

The user pressed on this directly: what does `BusinessObject` really embody — Identity, Status, Lifecycle,
Audit, Attachments, Notes, Workflow, Localization, ExtensionData, Concurrency, Permissions? Not all of these
live on the `BusinessObject` class itself; most are platform *services* a module's Application layer
consumes around a `BusinessObject`, not fields baked into the base class. This table is the map, kept
current as each gap gets closed:

| Guarantee | Lives in | Status (Business Partner, the first real consumer) |
|---|---|---|
| Identity, Status, Lifecycle, ExtensionData, Concurrency | `Platform.Core.BusinessObject` itself | Built since Phase 0 |
| Audit | `Platform.Audit.IAuditRecorder` | Wired — see `Modules.MasterData/README.md` |
| Workflow | `Platform.Workflow.IWorkflowEngine` | Wired — see `Modules.MasterData/README.md` |
| Permissions | `Platform.Security.IAuthorizationService` | Wired — see `Modules.MasterData/README.md` |
| Attachments | `Platform.Attachments.IAttachmentService` | Wired — see `Modules.MasterData/README.md` |
| Notes | *(not built yet)* | Not started — next slice |
| Localization | `Platform.Localization` | Wired at the platform/UI level (AR/EN, RTL); not yet exercised on Business Partner's own fields (see bilingual name fields, next slice after Notes) |

A module's Application layer calls each of these services explicitly, at the points a real business
process cares about (create, a status transition, an attachment upload) — `BusinessObject` itself doesn't
know any of them exist. That's deliberate: it's what keeps the kernel thin and lets each service be tested
and swapped independently (docs/architecture/01-architecture-foundation.md #1).
