# HadionERP — Architecture Gap Audit vs. SAP S/4HANA & Microsoft Dynamics 365 F&O

**Date**: 2026-07-15
**Author**: Claude Sonnet 5, acting in the capacity of a SAP/Dynamics enterprise architect performing a
maturity assessment on this codebase, per explicit user instruction.
**Method**: every finding below is grounded in the actual codebase state as of this date — grep results,
file paths, and read source, not speculation about what "should" exist. Where something is ambiguous, that
ambiguity is stated rather than guessed past. This file is a **snapshot**, not a living document — like
`PROGRESS.md`'s entries, do not silently edit past findings as the system evolves; add a new dated audit
section instead, or at minimum a "Resolved" note against the specific bullet with the date and the
`PROGRESS.md` entry that closed it.

**Purpose**: HadionERP is architecturally inspired by SAP S/4HANA (financial/project accounting) and
Dynamics 365 F&O (UI/navigation). This document is the honest gap list — what a real deployment of either of
those products has, out of the box, that this system does not yet have. It exists so future phases are
planned against reality, not against what's already been silently assumed to work.

---

## How to read this document

Each finding has:
- **What SAP/Dynamics has**: the real-world capability being compared against.
- **What HadionERP has**: the actual current state, cited to specific files.
- **Severity**: `Blocking` (a real business cannot go live without this), `Structural` (built but not
  actually load-bearing — code exists but nothing calls it, so it provides zero real value today),
  `Depth` (the current shape is a legitimate first cut, not a defect — more capability is a natural later
  phase, not a correction), or `Missing` (literally does not exist, not even a stub).
- **Recommended phase**: which future roadmap phase (see `docs/architecture/06-roadmap.md`, updated
  alongside this file) should close the gap.

---

## 1. Authentication & Identity — **Blocking** — ✅ **RESOLVED 2026-07-15**

> Real username/password authentication (JWT bearer tokens), a persisted Users admin surface, and a global
> default-deny authorization policy now exist — see `Modules.Identity/README.md` for the full build and
> `PROGRESS.md`'s "Real Authentication & Identity" entry (2026-07-15) for the live verification evidence.
> Every one of the 32 hardcoded actor-literal usages this finding counted is gone; every controller now
> resolves the real logged-in user via `PlatformApiController.CurrentActor`. Deferred (disclosed in the
> module's own README, not silently dropped): real OIDC/SSO federation, MFA, refresh-token rotation.

**What SAP/Dynamics has**: real user authentication (SAP: SSO/SAML/X.509 via SAP NetWeaver AS; Dynamics:
Azure AD/Entra ID OIDC), a logged-in user's identity flowing through every request, session management,
and role/permission resolution against that real identity.

**What HadionERP has**: nothing. Confirmed:
- Zero authentication packages in any `.csproj` in the solution (no `Microsoft.AspNetCore.Identity`, no
  JWT/OAuth/OIDC middleware package anywhere).
- Zero `[Authorize]` attributes on any controller.
- `Program.cs` calls `app.UseAuthorization()` but never calls `AddAuthentication`/`AddIdentity` — this makes
  `UseAuthorization()` a pass-through no-op, since nothing ever populates `HttpContext.User`.
- 32 hardcoded actor-literal usages (`"system/ui"`, `"system/approver"`, `"system/startup"`) stand in for a
  real logged-in user across every controller in the solution. Every audit entry, every workflow decision,
  every "who did this" is currently attributed to one of these three literals, never a real person.
- `Platform.Security.IActorRoleAssignmentStore` — the piece that would map a real user to their Roles — has
  exactly one implementation, `InMemoryActorRoleAssignmentStore`, configured as a hardcoded dictionary
  literal in `Program.cs`. There is no admin UI anywhere in `Apps.Shell` to assign a role to a user; role
  *assignment* (as opposed to role *definition*, which is real) does not exist as a product capability.

**Why this is the top-priority gap**: every other finding in this document is downstream of this one. Audit
trails, approval routing, and SoD conflict checks are all only as real as the identity behind the actor
string — right now they're all provably attributable to one of three hardcoded literals, which is fine for
a demo/development build but would not survive a single real compliance review.

**Recommended phase**: a new **Phase 0.5 — Identity & Access**, or folded into whichever phase precedes the
first real pilot deployment. Concretely: real ASP.NET Core Identity or an OIDC integration (Entra ID/Auth0/
Keycloak), a session/JWT pipeline, a real user-to-role assignment admin screen (which also finally gives
`ISodEngine.FindConflicts` — see §3 — something real to check), and retiring every hardcoded actor literal.

---

## 2. Multi-Company / Legal Entity Structure — **Missing**

**What SAP/Dynamics has**: a Company Code (SAP) / Legal Entity (Dynamics) as a first-class entity — separate
charts of accounts, number ranges, fiscal year variants, and posting rules per legal entity, with
consolidation across them.

**What HadionERP has**: no `Company`/`LegalEntity` domain entity anywhere in the codebase. "CompanyId" exists
only as a partitioning column on `NumberRangeCounterEntity` (number-range scoping), not a real entity — the
literal string `"C001"` is hardcoded as `const string companyId = "C001"` in roughly 15 places (one per
controller that creates a document). There is no company-selector UI, and nothing prevents (or would even
detect) two "different companies'" documents sharing one chart of accounts, one number range sequence, and
one audit log today, because there is only ever the one implicit company.

**Recommended phase**: **Phase 4 or later** — genuinely not needed until a second legal entity is a real
business requirement (the user's own stated construction/finance company is presumably one entity today).
Flagging this now so number ranges, GL accounts, and every future module are designed with a `CompanyId`
seam already present (they already carry the column) rather than retrofitted later — the seam exists, the
entity behind it does not.

---

## 3. Segregation of Duties — **Structural** (built, not load-bearing) — ✅ **RESOLVED 2026-07-15**

> `Modules.Identity.Application.UserService.AssignRoleAsync` now calls `ISodEngine.FindUnresolvedConflicts`
> on every role assignment — the first live call this engine has ever received. Live-verified: assigning two
> conflicting roles to the same user 409s with the registered rule's own reason text; supplying an override
> reason grants a logged exception (`ISodExceptionLog.Grant`) and lets the assignment through. See
> `Modules.Identity/README.md`. `ISodExceptionLog` itself is still the pre-existing in-memory singleton
> (disclosed there) — the check now runs for real; persisting the exception log across restarts is smaller
> follow-up work, not re-opened here.

**What SAP/Dynamics has**: SoD conflict detection that actually blocks or flags a role assignment at
assignment time (SAP GRC Access Control; Dynamics's Segregation of Duties feature in Security Configuration).

**What HadionERP has**: `Platform.Security.Sod.ISodEngine`/`SodEngine` is real, tested in isolation
(`Platform.Security.Tests`), and every module registers its own conflict rules (e.g.
`BusinessPartnerSecurity.MaintainerApproverConflict`) into a shared `ISodEngine` singleton in `Program.cs`.
**But `ISodEngine.FindConflicts` is never called from any Application service** — confirmed by grep across
every `Modules.*/Application/*.cs` file. The rules are real and the engine that would check them is real;
nothing in the live request path ever invokes it. This is a direct consequence of §1 (Authentication): SoD
conflict detection is meant to run at role-*assignment* time, and there is no role-assignment surface for it
to run against yet.

**Recommended phase**: closes automatically once §1's role-assignment admin UI exists — wire
`ISodEngine.FindConflicts` into that UI's "assign this role to this user" action.

---

## 4. Row-Level & Field-Level Security — **Structural** (dead code)

**What SAP/Dynamics has**: row-level security scoping records to a company/plant/cost-center/project a user
is authorized for (SAP's org-level authorization objects; Dynamics's record-level security via security
roles + organization hierarchy), and field-level masking for sensitive data (SAP's field-level authorization;
Dynamics's field security profiles) — e.g. an unauthorized user sees a payroll record exists but not the
salary figure.

**What HadionERP has**: `Platform.Security/FieldLevel/` and `Platform.Security/RowLevel/` exist as directories
with real types, but are referenced **nowhere** outside their own directory — confirmed by grep across the
entire solution. This is dead code today: defined, never consumed. No row in this system is currently scoped
by company/branch/project, and no field (there is nothing salary/IBAN-sensitive built yet — see Phase 4's HR/
Payroll) is masked.

**Recommended phase**: Row-level security becomes real the moment §2 (multi-company) exists — wire it in
alongside, not before, since there's no second company/branch to scope against yet. Field-level security
becomes real the moment Phase 4 (HR & Payroll) introduces its first genuinely sensitive field (salary, IBAN,
national ID).

---

## 5. Amount-Conditioned Approval Matrices — **Depth gap**

**What SAP/Dynamics has**: multi-branch approval workflows conditioned on document amount (SAP's Flexible
Workflow with a "release strategy" and value-based conditions; Dynamics's workflow condition rules — e.g.
"POs under 10,000 SAR need one approver, over 100,000 SAR need three").

**What HadionERP has**: `Platform.Workflow`'s engine (`WorkflowEngine`, `WorkflowDefinition`,
`WorkflowStepDefinition`) is real and capable of multi-step chains (proven by Vendor Prequalification's real
5-step Commercial→Legal→Technical→HSE→Quality workflow) and does support condition-gated steps via
`AttributeConstraints` (used to skip steps that don't apply to a given resource). But **no module anywhere
uses an amount threshold to choose between approval chains** — every workflow definition across every module
(Business Partner, GL Account, Item, Cost Center, Tax Code, Journal Entry, AP Invoice, Purchase Requisition,
RFQ, Purchase Order, GRN, Project) is a fixed one-step (or, for Vendor Prequalification, fixed five-step)
role-based chain regardless of the document's value. Phase 2's own stated exit criteria named "configurable
approval matrix" — what was actually built is *role-based*, not *amount-conditioned*; the engine underneath
already has the condition-gating primitive this would need, it has simply never been pointed at a document
amount.

**Recommended phase**: the first module that genuinely needs this (Purchase Order is the natural
candidate — a 5,000 SAR PO and a 5,000,000 SAR PO should not take the same one-step path) should build it as
a real `AttributeConstraints` condition on `PurchaseOrder.Total`, proving the pattern once, then apply it to
Journal Entry/AP Invoice/Purchase Requisition the same mechanical way. Natural fit for whichever phase next
touches Procurement or Finance approval depth.

---

## 6. Delegation & Escalation — **Structural** / **Missing**

**What SAP/Dynamics has**: substitute-approver delegation (an approver going on leave can delegate their
pending approvals; SAP's "Substitution" in Business Workflow, Dynamics's "Workflow delegate") and
time-based escalation (an approval sitting too long auto-escalates to the next level).

**What HadionERP has**:
- **Delegation**: `IDelegationRegistry`/`InMemoryDelegationRegistry` is genuinely wired into the live
  eligibility-decision path — `RoleBasedWorkflowEligibilityService` really does check
  `HasActiveDelegation` when deciding who can act on a workflow step. But `Program.cs` registers it as
  `new InMemoryDelegationRegistry()` with nothing ever added to it, and no API/UI anywhere calls a method to
  register a delegation. Structurally correct, operationally empty — the wiring is real, the feature has
  never been used because there's no way to use it yet.
- **Escalation**: `Platform.Workflow/Escalation/` (`EscalationCandidate.cs`, `EscalationScan.cs`) has zero
  references anywhere outside its own two files — not even wired into `WorkflowEngine`. Fully orphaned.

**Recommended phase**: Delegation just needs a small API/UI surface ("delegate my approvals to X for these
dates") — low effort once §1 (real users) exists, since delegation is meaningless between hardcoded actor
literals. Escalation needs a background job/scheduler (`Platform.Events`'s outbox-relay pattern is the
natural home) — reasonable to bundle with Phase 5 (Reporting/Analytics), which is the first phase that
implies any kind of scheduled background processing.

---

## 7. Notifications & Output Management — **Missing**

**What SAP/Dynamics has**: email notifications on workflow events (SAP Business Workflow's work item
notifications; Dynamics's Alerts/email templates), and a print/output management layer (SAP's output
determination + Smart Forms/Adobe Forms; Dynamics's document routing + report layouts) for generating a
PDF purchase order, invoice, or approval notice.

**What HadionERP has**: nothing — grepped for email/SMTP/notification-service/PDF-generation/print-output
across the entire `src/` tree, zero matches, not even a stub interface. `Platform.Reporting/` (which would be
the natural home for print/output) contains only a README describing intent — zero code.

**Recommended phase**: **Phase 5 — Reporting, Analytics & Mobile** already names "statutory reports" and
"management reports" as its scope; notifications and a basic print/output layer (starting with "email the
approver when something needs their decision," which directly improves the deferred-but-real workflow engine
from §5/§6) belong there too, and should probably come *before* the statutory-report generation work, since
it's the more foundational of the two.

---

## 8. ZATCA E-Invoicing — **Structural** (stub, unwired)

**What SAP/Dynamics has**: real ZATCA Phase 1 (QR-coded simplified tax invoice) and Phase 2 (XML/UBL
clearance integration) support, wired into actual invoice posting.

**What HadionERP has**: `Platform.Localization/Zatca/ZatcaSimplifiedInvoiceFields.cs` and
`ZatcaSimplifiedInvoiceQrBuilder.cs` exist as real, presumably-tested code, but are **never referenced
outside their own directory** — not wired into `Modules.Finance.Domain.APInvoice` or
`APInvoiceService` at all, despite `APInvoice`'s own doc comments explicitly mentioning ZATCA-compliant VAT.
No real invoice QR code or XML output is generated anywhere in a live request path today.

**Recommended phase**: Phase 1's roadmap entry already names "ZATCA e-invoicing Phase 1 (QR-coded compliant
invoices) live for AR" as in-scope — this was written into the roadmap but not actually completed; closing
it is a should-do before Phase 1 is truly called done, independent of any later phase. Phase 4's roadmap
entry already separately names ZATCA Phase 2 (XML/UBL) as in-scope for that phase.

---

## 9. Hijri Calendar — **Structural** (stub, unwired)

**What SAP/Dynamics has**: dual Hijri/Gregorian calendar support in date fields and date-driven business
logic (fiscal periods, aging), standard for a Saudi-market deployment of either product.

**What HadionERP has**: `Platform.Localization/Calendar/IHijriCalendarService.cs` and
`UmAlQuraHijriCalendarService.cs` exist but are never referenced outside their own directory. Every date
field across every page in `Apps.Shell` (7 occurrences of a plain `type="date"` HTML input) is a plain
Gregorian input with zero Hijri integration.

**Recommended phase**: low effort, high localization-authenticity payoff — a natural fit for whichever phase
does the next real UI pass (the paused UI/Visual Density Pass checkpoint, or a future one), since it's a
frontend-facing gap more than a backend one; the backend service already exists and works, it just isn't
called from anywhere the user would see it.

---

## 10. Multi-Currency — **Missing**

**What SAP/Dynamics has**: multi-currency documents with exchange-rate types and automatic translation to a
company's functional currency (SAP's parallel currencies on every FI document; Dynamics's currency exchange
rate tables).

**What HadionERP has**: zero `Currency` field on any Domain entity anywhere in the solution — the entire
system implicitly assumes SAR everywhere, never modeled as an explicit field, let alone a foreign-currency
document with an exchange rate.

**Recommended phase**: genuinely not needed until a real foreign-currency transaction is a business
requirement (a Saudi construction company's local vendors/subs are overwhelmingly SAR-denominated; foreign
equipment/material imports are the likely first real trigger). Flag for Phase 4 or later, and when it lands,
expect it to touch `Modules.Finance` (GL/AP/AR posting), `Modules.Procurement` (PO pricing), and
`Modules.MasterData` (a `Currency` lookup type — which, now that the Lookup Data engine exists per this
session's own checkpoint, is a five-minute addition once the actual currency *field* work is scoped).

---

## 11. Fiscal Year / Period Management — **Missing**

**What SAP/Dynamics has**: a real Fiscal Year Variant/calendar with period-open/period-close/period-lock
controls (SAP's fiscal year variant + posting period control; Dynamics's Ledger calendar + period status).

**What HadionERP has**: no `FiscalYear`/`Period` entity anywhere. Fiscal year is just
`DateTimeOffset.UtcNow.Year` passed directly into every `INumberRangeService.GetNext(...)` call (e.g.
`BusinessPartnerService.CreateAsync`) — a plain int for number-range scoping, not a real fiscal calendar.
Nothing prevents posting into a "closed" period because there is no concept of a period being open or closed.

**Recommended phase**: **Phase 1 depth** (already disclosed in `Modules.Finance/README.md`'s Deferred list
as "no period-close" — this audit confirms and elevates it) — genuinely blocking for any real financial
close process, so should land before a real production Finance go-live, likely as Finance-depth work
alongside Budget Control (already a named Phase 1 deferred item) rather than waiting for a later phase.

---

## 12. Reporting, Analytics & Dashboards — **Missing** (as scoped)

**What SAP/Dynamics has**: embedded analytical dashboards (SAP Fiori analytical apps/Embedded Analytics;
Dynamics's Power BI-embedded workspaces) surfacing KPIs, aging, WIP, and project profitability without
leaving the application.

**What HadionERP has**: `Platform.Reporting/` contains only a README. No dashboard/chart/KPI code exists
anywhere in `Apps.Shell` today (the sole "chart" grep hit across the frontend was the unrelated nav label
"Chart of Accounts").

**Recommended phase**: this is exactly **Phase 5 — Reporting, Analytics & Mobile**'s already-stated scope —
no roadmap change needed here, this finding just confirms the phase's premise is accurate (nothing has been
started early) rather than surfacing a new gap.

---

## 13. Extensibility & External Integration — **Missing** (as scoped)

**What SAP/Dynamics has**: a formal extension/plugin framework (SAP BTP extensibility model; Dynamics's
extension packages) and adapter layers for external systems (banks, government portals, e-signature).

**What HadionERP has**: `Platform.Extensibility/` and `Platform.Integration/` both contain only README files
describing intent (extension point registry/plugin manifest/sandboxed execution; ZATCA/GOSI/WPS/SADAD/bank/
e-signature adapters, respectively) — zero actual code in either. No webhook or API-key infrastructure exists
anywhere.

**Recommended phase**: this is exactly **Phase 6 — Extensibility Ecosystem & Advanced Capabilities**'s
already-stated scope — same as §12, this confirms rather than surfaces a new gap. Worth noting `GOSI`/`WPS`/
`SADAD` integration is *also* named under Phase 4 (Payroll) — when Phase 4 starts, `Platform.Integration`'s
placeholder is where those real adapters belong, not a bespoke one-off inside `Modules.Payroll`.

---

## 14. Attachments — blob storage & virus scanning — **Depth gap**

**What SAP/Dynamics has**: attachments in real object storage (SAP's Content Server/S3-compatible storage;
Dynamics's Azure Blob Storage-backed document management) with virus scanning and file-type restrictions on
upload.

**What HadionERP has**: `AttachmentContentRow.Content` is mapped as Postgres `bytea` — real, working,
correctly wired storage (this is not dead code, files genuinely upload/download/delete correctly, proven by
Vendor Prequalification's live-verified attachment cycle), but not real object storage, and there is no virus
scanning or file-type/extension restriction anywhere in `Platform.Attachments`. Already disclosed as a
deferred item in `Platform.Attachments/README.md` and `Modules.Procurement/README.md` — this audit confirms
it and elevates virus scanning specifically, which those READMEs don't call out by name.

**Recommended phase**: real object storage (S3-compatible, e.g. MinIO for self-hosted per the user's own
stated deployment plan) is worth doing whenever storage volume/cost first becomes a real constraint — not
urgent at current scale. Virus scanning (e.g. ClamAV) is a security-hygiene item that should land before any
external-facing deployment, independent of the storage-backend question.

---

## What this audit deliberately does NOT flag as a gap

To avoid over-scoping the roadmap with items that aren't real gaps:

- **The Configurable Lookup Data engine** (Country/BusinessRoleType/AddressType/UnitOfMeasure/
  SubcontractorTrade/SupplierTrade/ConsultantTrade, `Modules.MasterData`'s `LookupType`/`LookupValue`) — this
  was the exact gap the user identified this session ("customers, vendors — these words... hardcoded") and it
  is now **closed**, live-verified, documented in `Modules.MasterData/README.md`. Listed here only to
  contrast against the still-open findings above — it is not carried forward as a gap.
- **Trade being an unenforced suggestion, not a hard-validated field** — this is a deliberate design decision
  from the roadmap's own Phase 2 design (trades vary by discipline and grow organically), not an oversight.
- **The one-directional module dependency graph, Contracts-package boundary, and BusinessObject
  Draft→Submit→Approve lifecycle** — these are the architecture's own foundational decisions, working
  exactly as designed across every module built so far, not gaps.

---

## Summary table

| # | Gap | Severity | Recommended phase |
|---|---|---|---|
| 1 | Authentication & Identity | ~~Blocking~~ **Resolved 2026-07-15** | `Modules.Identity` |
| 2 | Multi-Company / Legal Entity | Missing | Phase 4+ |
| 3 | Segregation of Duties enforcement | ~~Structural~~ **Resolved 2026-07-15** | `Modules.Identity` |
| 4 | Row/Field-Level Security | Structural | Row: with #2. Field: Phase 4 (HR/Payroll) |
| 5 | Amount-conditioned approval matrices | Depth | Next Procurement/Finance approval work |
| 6 | Delegation & Escalation | Structural / Missing | Delegation: with #1. Escalation: Phase 5 |
| 7 | Notifications & Output Management | Missing | Phase 5 (before statutory reports) |
| 8 | ZATCA e-invoicing (Phase 1 QR) | Structural | Should-do to actually close Phase 1 |
| 9 | Hijri Calendar | Structural | Next UI pass |
| 10 | Multi-Currency | Missing | Phase 4+ |
| 11 | Fiscal Year / Period Management | Missing | Phase 1 depth (Finance, pre-production) |
| 12 | Reporting/Analytics/Dashboards | Missing (as scoped) | Phase 5 (already scoped correctly) |
| 13 | Extensibility & Integration adapters | Missing (as scoped) | Phase 6 (already scoped correctly) |
| 14 | Attachments: object storage & virus scan | Depth | Storage: as-needed. Virus scan: pre-external-deploy |

---
---

# Part 2 — Core Data Model & Module Completeness (added 2026-07-15, same day)

Part 1 above audited cross-cutting *platform* capabilities (auth, workflow depth, security enforcement,
localization). This part audits something different, per explicit user follow-up request: whether the
*business data model* of the modules already built (`Modules.MasterData`, `Modules.Finance`) actually carries
the fields real SAP FI/CO/MM and Dynamics 365 F&O carry on their equivalent master-data/finance objects, and
whether entire modules a real SAP/Dynamics deployment ships are missing from the roadmap altogether — not
just missing from code. Same evidence standard as Part 1: every finding below cites the actual current
Domain entity's field list, not a guess.

## 15. Business Partner — missing master-data fields — **Missing, undisclosed**

**What SAP/Dynamics has**: on the vendor/customer master — Payment Terms (SAP T052 / Dynamics "Terms of
payment"), a default Payment Method, bank account/IBAN details, a Credit Limit (customer-side), a
**Reconciliation Account** (the GL account a vendor's or customer's sub-ledger balance rolls up into — set
once on the master, defaulted onto every document instead of picked by hand each time), a Withholding Tax
indicator, and a "one-time vendor" flag for a vendor never expected to recur.

**What HadionERP has**: `BusinessPartner`'s complete field set today is `Name`/`NameArabic`/
`TaxRegistrationNumber` plus three child collections — `Addresses` (AddressType/Country/City/AddressLine),
`Contacts` (Name/JobTitle/Email/Phone), `BusinessRoles` (RoleType/Trade). None of the fields above exist
anywhere on it. This is why `APInvoiceService` requires `PayableAccountId` to be picked by hand on every
single invoice (already flagged in `Modules.Finance/README.md` as a deferred convenience) — there is no
concept of a default reconciliation account on the vendor to default it *from* in the first place; the
README's framing as "not yet defaulted" understates that the underlying master-data field doesn't exist at
all.

**Recommended phase**: **Phase 1 depth** — this is foundational Business Partner data, not a later-phase
concern; a real vendor master without payment terms or a bank account is unusable for actual AP processing.
Should land before/alongside closing §16 below (Payment recording), since the two are the same real-world
capability split across master data and transactional posting.

---

## 16. AP Payment Recording & Cash/Bank Management — **Blocking** (the single biggest finding in this audit)

**What SAP/Dynamics has**: a complete AP cycle — Invoice → Payment Proposal/Run → Payment (clears the payable,
credits a bank account) → Bank Reconciliation. House Bank master data (SAP FI-BL; Dynamics Cash and bank
management) holds the bank accounts payments actually post against.

**What HadionERP has**: `APInvoice.Post()` creates exactly one Journal Entry — Debit Expense, Credit Payable.
**Nothing in the entire codebase ever debits Payable and credits Bank.** Confirmed by a solution-wide grep:
zero hits for `Payment`/`Disbursement`/`BankAccount`/`HouseBank` as class or concept names anywhere. An
invoice can be created, approved, and posted — and then the system has no way to ever represent that it was
paid. This is more than a missing convenience field; it means **the AP cycle this system implements today
stops one real-world step short of complete**, and no report or balance in this system can ever distinguish
an unpaid payable from a paid one.

`docs/architecture/06-roadmap.md` line 29 names "Cash/Bank" as in-scope for Phase 1 alongside GL/AP/AR, and
`Modules.Finance/README.md`'s Deferred section mentions "AR/Cash-Bank... genuinely later work" — but only as
a one-word label; neither document itemizes that this specifically means "there is currently no way to record
a vendor payment," which is a materially more urgent gap than the label suggests.

**Recommended phase**: **Phase 1 depth, should precede any real Finance production use** — arguably the
single highest-priority *data-model* gap in this whole audit (Part 1's Authentication finding is still the
highest-priority *platform* gap overall). A minimal real version: a `BankAccount` master entity (owned by
`Modules.MasterData` or a new `Modules.Finance` sub-area), a `Payment`/`APPayment` Business Object
(Vendor + Bank Account + Amount + Date, posts Debit Payable/Credit Bank, links back to the invoice(s) it
settles), and — later, not blocking — real bank reconciliation.

---

## 17. GL Account — no Document Type concept — **Missing, undisclosed**

**What SAP/Dynamics has**: a Document Type (SAP: KR for vendor invoice, DR for customer invoice, SA for
general journal, etc.) sitting between "which Business Object" and "which number range" — it groups several
posting scenarios under shared numbering and field-control rules (which accounts are postable, which fields
are required) rather than each document shape inventing its own number range from scratch.

**What HadionERP has**: number-ranging is purely 1:1 with `BusinessObjectType` today (`JournalEntry` →
`FIN-JE`, `APInvoice` → `FIN-AP`, etc. — one range per BO type, no intermediate grouping concept). This works
fine at the current small number of document shapes; it's a real gap only once the number of distinct posting
scenarios grows enough that SAP's grouping actually earns its complexity.

**Recommended phase**: not urgent — revisit only if/when the number of distinct Finance document shapes grows
enough to make per-BO-type number ranges unwieldy. Flagged here so it isn't silently assumed equivalent to
SAP's model if a future integration or report expects a real Document Type field.

---

## 18. Cost Center / Controlling — Profit Center & Internal Order — **Missing, partially subsumed**

**What SAP/Dynamics has**: Profit Center as a *separate* Controlling dimension from Cost Center (a Cost
Center answers "where was this cost incurred," a Profit Center answers "which business unit/segment is this
P&L attributable to" — the same transaction often carries both). Internal Order — a temporary, project- or
event-scoped cost collector, distinct from a permanent Cost Center.

**What HadionERP has**: only `CostCenter` (Code/Name/NameArabic/ParentCostCenterId/IsPostable/IsActive) — no
separate Profit Center dimension, no Internal Order concept.

**Recommended phase**: Profit Center is genuinely deferred-worthy (Phase 4+, once segment/business-unit
reporting is a real requirement). **Internal Order's natural home is likely `Modules.ProjectManagement`'s
WBS Element** (already Phase 3, in progress) rather than a new Finance-Core gap — a WBS element already
carries `IsAccountAssignmentElement`, which is structurally the same real-world job an Internal Order does in
SAP (a temporary, project-scoped cost collector). Worth confirming this intentionally when Phase 3's WBS
cost-posting depth is built, rather than accidentally building two competing concepts for the same job.

---

## 19. Withholding Tax & Tax Jurisdiction Code — **Missing, low priority**

**What SAP/Dynamics has**: a Withholding Tax type/code as a distinct concept from output/input VAT (relevant
in KSA for specific vendor categories under ZATCA rules), and a Tax Jurisdiction Code for multi-jurisdiction
tax regimes.

**What HadionERP has**: only the one `TaxCode` entity (`TaxCodeCode`/`TaxCodeName`/`Rate`/`TaxType`
Standard/ZeroRated/Exempt) — no withholding-tax concept, no jurisdiction code.

**Recommended phase**: Withholding tax is a real KSA requirement, worth Phase 1/4 depth once AP payment
recording (§16) exists to actually withhold *against*. Jurisdiction code is low priority — KSA VAT is a
single flat national rate, not a multi-jurisdiction regime like US sales tax, so this is the least urgent
item in this entire audit.

---

## 20. Fixed Assets — **Missing, entire module absent from both code and roadmap**

**What SAP/Dynamics has**: SAP FI-AA / Dynamics "Fixed assets" — asset master records, acquisition/
depreciation/disposal postings, depreciation areas, asset classes.

**What HadionERP has**: nothing. Zero `Asset`/`FixedAsset` class anywhere in the solution, and Fixed Assets
is not named anywhere in `docs/architecture/06-roadmap.md`'s Phase 0-6 list.

**Why this matters specifically for this user's business**: a construction/EPC company owns significant
capital equipment (cranes, trucks, generators, formwork, site equipment) — Fixed Assets is not a generic
"nice to have," it's a real, load-bearing gap for the industry this ERP is built for.

**Recommended phase**: new roadmap item, natural fit for Phase 4 alongside HR/Payroll (both are "the other
major Finance-adjacent modules a real deployment needs before go-live"), or its own checkpoint between
Phase 3 and Phase 4 given the construction-specific relevance.

---

## 21. Inventory / Warehouse Management — **Missing, entire module absent from both code and roadmap**

**What SAP/Dynamics has**: SAP MM-IM / Dynamics "Inventory management" — on-hand quantity per warehouse/bin,
goods movements, stock valuation.

**What HadionERP has**: nothing — and this is a genuine internal inconsistency, not just an absence.
`Item.cs`'s own doc comment says a Stock item is "warehouse-tracked (**Inventory owns on-hand quantity**,
this module only owns the master record)" — this sentence presupposes an Inventory module that doesn't exist
anywhere in the codebase or the roadmap. Procurement's Goods Receipt Note records that goods were received
against a PO, but **never actually increments any stock balance anywhere** — there is no on-hand-quantity
field on anything, no warehouse/bin entity, no goods-movement concept.

**Recommended phase**: new roadmap item. Natural sequencing: after Phase 3's Construction module exists (site
material consumption is the real driver of on-hand tracking for a construction company — knowing how much
rebar/cement is on a given site), likely Phase 4 or its own checkpoint.

---

## 22. Plant/Equipment Maintenance — **Missing, entire module absent from both code and roadmap**

**What SAP/Dynamics has**: SAP PM (Plant Maintenance) — equipment master, maintenance plans/orders, breakdown
tracking — distinct from Fixed Assets' financial depreciation concern (Fixed Assets answers "what is this
crane worth on the books," Plant Maintenance answers "when does this crane need its next service").

**What HadionERP has**: nothing, not named anywhere in the roadmap.

**Recommended phase**: high relevance for a construction company (crane/vehicle/generator maintenance
scheduling directly affects site safety and uptime) but genuinely a later-phase concern — natural pairing
with §20 (Fixed Assets), since real Plant Maintenance usually references the same equipment master Fixed
Assets would introduce. Consider scoping both together rather than twice.

---

## 23. Real Estate / Site-Land Management — **Open question, not a confirmed gap**

**What SAP/Dynamics has**: SAP RE-FX — land/site/lease management as a distinct object model from Fixed
Assets or Projects.

**What HadionERP has**: nothing, not named anywhere. Unlike §20-22, this is flagged as a **question, not a
confirmed gap** — whether this is genuinely needed depends on the user's own business model (does the company
own/lease land or site facilities as a distinct concern from the projects built on them, or does every site
map 1:1 to a `Modules.ProjectManagement` Project with no separate real-estate concern?). Worth a direct
question to the user before scoping, not worth guessing into the roadmap speculatively.

---

## Part 2 summary table

| # | Gap | Severity | Recommended phase |
|---|---|---|---|
| 15 | Business Partner master data (Payment Terms, Bank/IBAN, Credit Limit, Recon Account, WHT) | Missing | Phase 1 depth, with #16 |
| 16 | AP Payment Recording & Cash/Bank Management | **Blocking** | Phase 1 depth, pre-production — highest-priority data-model gap in this audit |
| 17 | GL Document Type concept | Missing | Not urgent — revisit if document-shape count grows |
| 18 | Profit Center & Internal Order | Missing (Internal Order likely subsumed by WBS) | Profit Center: Phase 4+. Internal Order: confirm against Phase 3's WBS cost depth |
| 19 | Withholding Tax & Jurisdiction Code | Missing | WHT: Phase 1/4, after #16. Jurisdiction: low priority |
| 20 | Fixed Assets (entire module) | Missing | New roadmap item, Phase 4 or own checkpoint — construction-relevant |
| 21 | Inventory/Warehouse Management (entire module) | Missing | New roadmap item, after Phase 3 Construction |
| 22 | Plant/Equipment Maintenance (entire module) | Missing | New roadmap item, pair with #20 |
| 23 | Real Estate/Site-Land Management | Open question | Ask the user before scoping |
