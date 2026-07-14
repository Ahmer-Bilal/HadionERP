# Platform.Notes

The platform's free-text note capability — the last of the guarantees `Platform.Core.BusinessObject` was
supposed to carry (Identity, Status, Lifecycle, Audit, Attachments, Notes, Workflow, Localization,
ExtensionData, Concurrency, Permissions) that had never been built at all, until now. See
`Platform.Core/README.md`'s guarantee table for the full picture.

## What's built

- `Note`: one free-text note, linked to a Business Object via a flat `(BusinessObjectType,
  BusinessObjectId)` pair — the same polymorphic-link shape `Platform.Attachments.AttachmentMetadata` and
  `Platform.Workflow.WorkflowInstance` already use. Deliberately **append-only/delete-only** — there is no
  `UpdateText` — matching the platform's existing "correct by reversal, not by silent edit" principle
  (docs/architecture/02-business-object-model.md §1.1): a note is what someone actually said at the time.
- `INoteRepository`: the storage-agnostic persistence port, same pattern as
  `Platform.Attachments.IAttachmentRepository`. First implementation:
  `Modules.MasterData.Infrastructure.EfNoteRepository`.
- `INoteService`/`NoteService`: the single entry point modules call — validates non-empty text and a
  2000-character ceiling (long enough for a real explanation, short enough that a note stays a note —
  attaching a whole document is what `Platform.Attachments` is for).

## First real consumer: Modules.MasterData's Business Partner

Free-text notes on a vendor/customer record (a phone call summary, a reason for a status change beyond
what a workflow decision comment captures) — see `Modules.MasterData/README.md` for the specifics.

## Deferred (disclosed, not hidden)

- **Row/field-level security** — `Platform.Security.RowLevel`/`FieldLevel` aren't wired into this
  capability; anyone who can read a Business Object today can read its notes.
- **Mentions/notifications** (e.g. "@mention a colleague") — needs `Platform.Events`/notification delivery
  wired up first; a real but later extension, not this slice.
- **Rich text/attachments on a note** — plain text only for now; a note that needs a document should
  reference an actual `Platform.Attachments` upload instead of trying to embed one.
