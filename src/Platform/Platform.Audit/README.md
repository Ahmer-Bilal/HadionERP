# Platform.Audit

Immutable, tamper-evident audit log with field-level change tracking. See
docs/architecture/03-platform-services.md #5.

Every create / update / status-transition / delete-attempt on an audited entity is recorded with who, what
(field-level before/after), when, from where (IP/device), and why (summary + optional correlation id to a
workflow/approval). Records are hash-chained so a retroactive edit is computationally evident — the same
spirit as SAP's change-document framework, and a real requirement for financial/statutory defensibility in
Saudi Arabia (ZATCA, external auditors).

## What's built
- `AuditEntry`: one permanent record (who/what/when/from-where/why), carrying `FieldValueChange`s for the
  before/after of each changed field.
- `Hashing/AuditHasher`: SHA-256 over a deterministic canonical string of the entry's immutable content plus
  the PREVIOUS entry's hash. That link is what makes tampering detectable: changing any old field changes
  its hash, which changes every later entry's `PreviousHash`.
- `IAuditLog`/`InMemoryAuditLog`: append-only store. `Append` computes the entry's hash from the current
  tail (callers never supply `Hash`/`PreviousHash`). `VerifyChain()` recomputes every hash from the genesis
  entry forward and returns the first entry that disagrees with its recomputed hash — the tamper-evidence
  check. There is no Update/Delete method, by design (§5: append-only).
- `IAuditRecorder`/`AuditRecorder`: the friendly facade modules call (`RecordCreate`, `RecordFieldUpdate`,
  `RecordStatusTransition`, `RecordDeleteAttempt`). Modules never call `IAuditLog` directly — same
  single-entry-point pattern as `IIntegrationEventPublisher` for cross-module events.

Gateway.Api records one real, permanent operational audit entry at boot (`Platform.System.ApplicationStarted`
— "application started") and surfaces `audit.entries` + `audit.chainValid` on the System Status page, proving
the pipeline works in the actual running application, not just in tests.

## Deferred
- **A real append-only audit table** (§5: "no UPDATE/DELETE grants on the audit schema, enforced at the DB
  role level") — `InMemoryAuditLog` proves the chain mechanics first; a real deployment needs an actual
  database with the append-only constraint enforced at the DB role level, and the audit write committed in
  the same transaction as the business change.
- **Automatic audit at every lifecycle transition** — no business module exists yet to drive it. When one is
  built, its Application layer will call `IAuditRecorder` at each transition (the same way it will call
  `IIntegrationEventPublisher`); Gateway.Api records the one boot entry today to prove the live pipeline.
- **Retention/archiving** (§5): configurable per data classification, enforced by an archiving job, not
  manual deletion — needs the archiving job, not built yet.
- **Compliance exports** (§5): feeding the audit trail into ZATCA/GOSI/Ministry reporting via
  `Modules.Reporting` — needs those modules, not built yet.
