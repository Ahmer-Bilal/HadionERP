# Platform.Attachments

The platform's file-attachment capability — one of the guarantees a real Business Object is supposed to
carry (Identity, Status, Lifecycle, Audit, Attachments, Notes, Workflow, Localization, ExtensionData,
Concurrency, Permissions) that had never actually been built at all until now, caught when the user pressed
on what `Platform.Core.BusinessObject` really embodies versus what was still just a promise on paper.

## What's built

- `AttachmentMetadata`: one uploaded file's metadata (filename, content type, size, who/when), linked to a
  Business Object via a flat `(BusinessObjectType, BusinessObjectId)` pair — the same polymorphic-link
  shape `Platform.Workflow.WorkflowInstance` already uses, not `Platform.Core.BusinessObjectReference`
  (that type's `RelationKind` doesn't apply — an attachment always belongs to exactly the one record it was
  uploaded against). File bytes are deliberately NOT a property on this type — see its own doc comment.
- `IAttachmentRepository`: the storage-agnostic persistence port, same "kernel defines the port, a module
  with a real database implements it" pattern as `Platform.Workflow.IWorkflowInstanceRepository` and
  `Platform.Core.NumberRanges.INumberRangeService`. First implementation:
  `Modules.MasterData.Infrastructure.EfAttachmentRepository`.
- `IAttachmentService`/`AttachmentService`: the single entry point modules call (mirrors
  `Platform.Audit.IAuditRecorder`) — upload validation lives here so every module gets the same rules:
  a 10 MB size ceiling and a content-type **allowlist** (PDF, PNG, JPEG, Word, Excel), not a denylist —
  trying to enumerate every dangerous file extension is a losing game; only admitting known-safe
  document/image types isn't. No executable, script, or macro-capable format is ever accepted.

## First real consumer: Modules.MasterData's Business Partner

Business Partner's onboarding documents (CR copy, GOSI certificate, ISO certificates, bank letter, HSE
policy — exactly the supporting documents Vendor Prequalification's future design already anticipated, see
`docs/architecture/06-roadmap.md` Phase 2) are the first real use case. `BusinessPartnerService` gets
`Upload`/`List`/`Download`/`Delete` methods and `BusinessPartnersController` gets matching endpoints;
see `Modules.MasterData/README.md` for the specifics.

## Deferred (disclosed, not hidden)

- **Real blob storage** (S3/Azure Blob) — file bytes are stored directly in Postgres (a `bytea` column, in
  their own table separate from metadata so listing attachments never loads them) for now, the same
  "one real datastore now, swap for a specialized store later behind the same interface" choice already
  made for `WorkflowInstance`. Revisit once file volume/size makes this impractical.
- **Virus/malware scanning** — needs a real deployment target (an actual scanning service/sandbox) to mean
  anything; the content-type allowlist is a first line of defense, not a substitute for one.
- **Row/field-level security on attachments** — `Platform.Security.RowLevel`/`FieldLevel` aren't wired into
  this capability; anyone who can read a Business Object today can read its attachments.
- **Virus scanning aside, no antivirus/content-sniffing validation of the actual file bytes** — only the
  declared `Content-Type` header is checked, which a malicious client could lie about. Acceptable for this
  stage (internal users, not a public upload surface) but revisit before any external-facing use.
